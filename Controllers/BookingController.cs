using AmarShowsBook.Data;
using AmarShowsBook.Helpers;
using AmarShowsBook.Models;
using AmarShowsBook.Models.ViewModels;
using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace AmarShowsBook.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger;
        private readonly RbacService _rbacService;

        public BookingController(
            ApplicationDbContext context,
            IActivityLogger activityLogger,
            RbacService rbacService)
        {
            _context = context;
            _activityLogger = activityLogger;
            _rbacService = rbacService;
        }
public async Task<IActionResult> Ticket(long id)
{
    var booking = await _context.BookingDrafts
        .FirstOrDefaultAsync(x => x.Id == id);

    if (booking == null)
        return NotFound();

    var schedule = await _context.ShowSchedules
        .Include(x=>x.Movie)
        .Include(x=>x.StandupShow)
    .Include(x=>x.LiveStream)
    .Include(x=>x.Location)
    .Include(x=>x.Screen)
    .FirstOrDefaultAsync(x=>x.Id==booking.ScheduleId);

    ViewBag.Schedule = schedule;
    await SetTheaterViewBag(schedule);
    ViewBag.User = await _context.Users.FirstOrDefaultAsync(x=>x.Id==booking.UserId);
    ViewBag.TicketUrl = BuildAbsoluteUrl($"/Booking/Ticket/{booking.Id}");

    return View("MobileTicket", booking);
}

[Route("Booking/TicketByBooking/{id:long}")]
public async Task<IActionResult> TicketByBooking(long id)
{
    var bookingSummary =
    await _context.VwBookingCompleteDetails
    .AsNoTracking()
    .FirstOrDefaultAsync(x=>x.BookingId==id);

    if(bookingSummary==null)
    {
        return NotFound();
    }

    var userIdText =
    HttpContext.Session.GetString("UserId");

    if(!long.TryParse(userIdText,out var currentUserId))
    {
        return RedirectToAction("Login","Auth");
    }

    var canViewAnyTicket =
        _rbacService.HasPermission((int)currentUserId, "BOOKING", "VIEW") ||
        _rbacService.HasPermission((int)currentUserId, "ADMIN", "VIEW") ||
        _rbacService.HasAnyActiveRole((int)currentUserId, "AMAR_SUPER_ADMIN", "AMAR_ADMIN", "ADMIN");

    if(currentUserId!=bookingSummary.UserId && !canViewAnyTicket)
    {
        return StatusCode(
        StatusCodes.Status403Forbidden,
        "You do not have access to this ticket.");
    }

    var confirmedBooking =
    await _context.Bookings
    .AsNoTracking()
    .FirstOrDefaultAsync(x=>x.Id==id);

    var confirmedScheduleId =
    confirmedBooking?.ScheduleId;

    var schedule =
    await _context.ShowSchedules
    .Include(x=>x.Movie)
    .Include(x=>x.StandupShow)
    .Include(x=>x.LiveStream)
    .Include(x=>x.Location)
    .Include(x=>x.Screen)
    .FirstOrDefaultAsync(x=>
        confirmedScheduleId.HasValue
        ? x.Id==confirmedScheduleId.Value
        : x.StartTime==bookingSummary.StartTime);

    if(schedule==null)
    {
        schedule =
        await _context.ShowSchedules
        .Include(x=>x.Movie)
        .Include(x=>x.StandupShow)
        .Include(x=>x.LiveStream)
        .Include(x=>x.Location)
        .Include(x=>x.Screen)
        .OrderByDescending(x=>x.StartTime)
        .FirstOrDefaultAsync();
    }

    var user =
    await _context.Users
    .FirstOrDefaultAsync(x=>x.Id==bookingSummary.UserId);

    var confirmedTransactionId = confirmedBooking?.TransactionId;
    var transaction =
    await _context.Transactions
    .AsNoTracking()
    .Where(x=>x.BookingId==id || (confirmedTransactionId.HasValue && x.Id==confirmedTransactionId.Value))
    .OrderByDescending(x=>x.CompletedAt)
    .ThenByDescending(x=>x.CreatedAt)
    .FirstOrDefaultAsync();

    var tickets =
    await _context.Tickets
    .AsNoTracking()
    .Where(x=>x.BookingId==id)
    .OrderBy(x=>x.SeatNumber)
    .ToListAsync();

    if(!tickets.Any() && confirmedBooking!=null)
    {
        await EnsureTicketsForConfirmedBooking(
        confirmedBooking,
        bookingSummary,
        user);

        tickets =
        await _context.Tickets
        .AsNoTracking()
        .Where(x=>x.BookingId==id)
        .OrderBy(x=>x.SeatNumber)
        .ToListAsync();
    }

    var ticket =
    new BookingDraft
    {
        Id=id,
        UserId=bookingSummary.UserId,
        ScheduleId=schedule?.Id ?? 0,
        SeatNumbers=bookingSummary.SeatNumbers ?? "",
        TotalAmount=bookingSummary.PayableAmount ?? bookingSummary.TotalAmount,
        Status=bookingSummary.BookingStatus,
        CreatedAt=bookingSummary.BookedAt
    };

    ViewBag.Schedule=schedule;
    await SetTheaterViewBag(schedule);
    ViewBag.User=user;
    ViewBag.BookingRef=bookingSummary.BookingRef;
    ViewBag.BookingSummary=bookingSummary;
    ViewBag.Transaction=transaction;
    ViewBag.Tickets=tickets;
    ViewBag.TicketUrl=BuildAbsoluteUrl($"/Booking/TicketByBooking/{id}");

    return View("MobileTicket",ticket);
}

private async Task EnsureTicketsForConfirmedBooking(
    Booking booking,
    VwBookingCompleteDetails bookingSummary,
    User? user)
{
    if(await _context.Tickets.AnyAsync(x=>x.BookingId==booking.Id))
    {
        return;
    }

    var now =
    DateTime.UtcNow;

    var seats =
    (bookingSummary.SeatNumbers ?? string.Empty)
    .Split(',',StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Where(x=>!string.IsNullOrWhiteSpace(x))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToList();

    if(!seats.Any())
    {
        seats.Add("GENERAL");
    }

    foreach(var seatNumber in seats)
    {
        _context.Tickets.Add(
        new Ticket
        {
            BookingId=booking.Id,
            TicketNumber=$"TKT-{booking.Id}-{seatNumber}-{Guid.NewGuid():N}".Substring(0,32),
            AttendeeName=user?.Name ?? bookingSummary.UserName,
            SeatNumber=seatNumber,
            QrCode=$"BOOKING:{booking.BookingRef};SEAT:{seatNumber}",
            TicketStatus="ACTIVE",
            IssuedAt=now,
            CreatedAt=now,
            UpdatedAt=now,
            QrGeneratedAt=now,
            ValidationStatus="NOT_SCANNED"
        });
    }

    await _context.SaveChangesAsync();
}

        // ==========================================
        // SEATS
        // ==========================================

 [Route("Booking/Seats/{id?}")]
public async Task<IActionResult> Seats(int? id)
{
    ShowSchedule? schedule;

    // If no id passed -> automatically select nearest/current show
    if(!id.HasValue)
    {
        schedule =
        await _context.ShowSchedules
        .Include(x=>x.Movie)
        .Include(x=>x.StandupShow)
        .Include(x=>x.LiveStream)
        .Include(x=>x.Location)
        .Where(x=>x.StartTime >= DateTime.UtcNow)
        .OrderBy(x=>x.StartTime)
        .FirstOrDefaultAsync();

        if(schedule == null)
            return NotFound();

        return Redirect(
        $"/Booking/Seats/{schedule.Id}");
    }

    schedule =
    await _context.ShowSchedules
    .Include(x=>x.Movie)
    .Include(x=>x.StandupShow)
    .Include(x=>x.LiveStream)
    .Include(x=>x.Location)
    .Include(x=>x.Screen)
    .FirstOrDefaultAsync(x=>x.Id==id);

    if(schedule==null)
        return NotFound();



    var availableUntil =
    DateTime.UtcNow.Date.AddDays(7).AddDays(1);

    var availableDates =
    await _context.ShowSchedules
    .Where(x=>

        x.StartTime >= DateTime.UtcNow
        &&

        x.StartTime < availableUntil

        &&

        x.Type == schedule.Type

        &&

        (

            (schedule.Type=="Movie"
            &&
            x.MovieId==schedule.MovieId)

            ||

            (schedule.Type=="Standup"
            &&
            x.StandupShowId==
            schedule.StandupShowId)

            ||

            (schedule.Type=="Live"
            &&
            x.LiveStreamId==
            schedule.LiveStreamId)

        )

    )
    .OrderBy(x=>x.StartTime)
    .ToListAsync();



    ViewBag.AvailableDates =
    availableDates;
    await SetTheaterViewBag(schedule);



    bool seatsExist =
    await _context.ScreenSeats
    .AnyAsync(x=>x.ScheduleId==schedule.Id);

    if(!seatsExist)
    {
        await GenerateSeats(
        schedule.Id,
        schedule.Type);
    }



    var seats =
    await
    (
        from s in _context.ScreenSeats

        where s.ScheduleId==schedule.Id

        join l in _context.SeatLocks
        on s.Id equals l.ScreenSeatId
        into lockGroup

        from lockSeat in
        lockGroup.DefaultIfEmpty()

        select new SeatVM
        {
            SeatId=s.Id,

            Row=s.SeatRow,

            Number=s.SeatNumber,

            Price=s.SeatPrice,

            Category=s.SeatCategory,

            IsBooked=
            lockSeat!=null
            &&
            lockSeat.LockStatus=="CONFIRMED",

            IsLocked=
            lockSeat!=null
            &&
            lockSeat.LockStatus=="LOCKED"
        }

    ).ToListAsync();



    ViewBag.Seats = seats;



    var listing =
    await _context.HomeShows
    .AsNoTracking()
    .FirstOrDefaultAsync(
    x=>x.ScheduleId==schedule.Id);

    ViewBag.TrailerUrl=
    listing?.TrailerUrl;



    var durationMinutes =
    schedule.Movie?.Duration
    ??
    schedule.StandupShow?.Duration
    ??
    schedule.LiveStream?.Duration
    ??
    0;

    ViewBag.TrailerStartSeconds =
    durationMinutes>2
    ?
    Random.Shared.Next(
    15,
    Math.Max(
    16,
    (durationMinutes*60)-60))
    :
    0;



    return View(schedule);
}
        // ==========================================
        // LOCK SEATS
        // ==========================================

        [HttpPost]
        public async Task<IActionResult>
        LockSeats(
        [FromBody]
        SeatLockRequest request)
        {
            long userId=
            Convert.ToInt64(
            HttpContext.Session.GetString("UserId"));

            foreach(var seatId in request.SeatIds)
            {
                bool exists=

                await _context.SeatLocks.AnyAsync(
                x=>

                x.ScheduleId==
                request.ScheduleId

                &&

                x.ScreenSeatId==
                seatId

                &&

                (
                x.LockStatus=="LOCKED"
                ||
                x.LockStatus=="CONFIRMED"
                ));

                if(exists)
                {
                    return Json(new
                    {
                        success=false,
                        message="Seat already booked"
                    });
                }

                _context.SeatLocks.Add(

                new SeatLock
                {
                    UserId=userId,
                    ScheduleId=request.ScheduleId,
                    ScreenSeatId=seatId,
                    LockedAt=DateTime.UtcNow,
                    ExpiresAt=DateTime.UtcNow.AddMinutes(5),
                    LockStatus="LOCKED"
                });
            }

            await _context.SaveChangesAsync();

            // var booking=
            // new BookingDraft
            // {
            //     UserId=userId,
            //     ScheduleId=request.ScheduleId,
            //     SeatNumbers=
            //     string.Join(",",request.SeatIds),

            //     TotalAmount=
            //     request.TotalAmount,

            //     Status="PENDING",

            //     CreatedAt=
            //     DateTime.UtcNow
            // };
            var seatNames=

await _context.ScreenSeats
.Where(
x=>request.SeatIds.Contains(x.Id))
.Select(
x=>x.SeatRow + x.SeatNumber)
.ToListAsync();


var booking=
new BookingDraft
{
    UserId=userId,

    ScheduleId=
    request.ScheduleId,

    SeatNumbers=
    string.Join(
    ",",
    seatNames),

    TotalAmount=
    request.TotalAmount,

    Status=
    "PENDING",

    CreatedAt=
    DateTime.UtcNow
};

            _context.BookingDrafts.Add(
            booking);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success=true,
                bookingId=booking.Id
            });
        }

// ==========================================
// BOOKING DETAILS
// ==========================================

// public async Task<IActionResult>
// Details(long id)
// {
//     var booking =

//     await _context
//     .BookingDrafts
//     .FirstOrDefaultAsync(
//     x=>x.Id==id);

//     if(booking==null)
//     {
//         return NotFound();
//     }

//     var schedule=

//     await _context
//     .ShowSchedules
//     .Include(x=>x.Movie)
//     .Include(x=>x.StandupShow)
//     .Include(x=>x.LiveStream)
//     .Include(x=>x.Location)
//     .FirstOrDefaultAsync(
//     x=>x.Id==
//     booking.ScheduleId);

//     ViewBag.Schedule=
//     schedule;

//     return View(
//     booking);
// }
public async Task<IActionResult>
Details(long id)
{
    var booking=

    await _context
    .BookingDrafts
    .FirstOrDefaultAsync(
    x=>x.Id==id);

    if(booking==null)
        return NotFound();


    var schedule=

    await _context
    .ShowSchedules
    .Include(x=>x.Movie)
    .Include(x=>x.StandupShow)
    .Include(x=>x.LiveStream)
    .Include(x=>x.Location)
    .Include(x=>x.Screen)
    .FirstOrDefaultAsync(
    x=>x.Id==booking.ScheduleId);


    var user=

    await _context
    .Users
    .FirstOrDefaultAsync(
    x=>x.Id==booking.UserId);


    ViewBag.Schedule=
    schedule;
    await SetTheaterViewBag(schedule);

    ViewBag.User=
    user;

    var wallet =
    await _context.VwWalletSummaries
    .AsNoTracking()
    .FirstOrDefaultAsync(x=>x.UserId==booking.UserId);

    ViewBag.Wallet=
    wallet;

    ViewBag.WalletAmountUsed=
    GetWalletUsage(booking.Id);

    ViewBag.CouponDiscount=
    GetCouponDiscount(booking.Id);

    ViewBag.CouponCode=
    HttpContext.Session.GetString(
    GetCouponCodeSessionKey(booking.Id));

    ViewBag.AvailableCoupons=
    await GetActiveCouponSummaries();

    return View(
    booking);
}
        // ==========================================
        // PAYMENT PAGE
        // ==========================================

        public async Task<IActionResult>
        Payment(long bookingId)
        {
            var booking=

            await _context
            .BookingDrafts
            .FindAsync(bookingId);

            if(booking==null)
                return NotFound();

            var walletAmountUsed =
            GetWalletUsage(booking.Id);

            ViewBag.WalletAmountUsed =
            walletAmountUsed;

            var couponDiscount =
            GetCouponDiscount(booking.Id);

            ViewBag.CouponDiscount =
            couponDiscount;

            ViewBag.PayableAmount =
            Math.Max(0,booking.TotalAmount-walletAmountUsed-couponDiscount);

            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> ApplyWallet(
        long bookingId,
        decimal walletAmount)
        {
            var booking =
            await _context.BookingDrafts
            .FindAsync(bookingId);

            if(booking==null)
            {
                return NotFound();
            }

            var userIdText =
            HttpContext.Session.GetString("UserId");

            if(!long.TryParse(userIdText,out var userId) || userId!=booking.UserId)
            {
                return RedirectToAction("Login","Auth");
            }

            var wallet =
            await _context.VwWalletSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(x=>x.UserId==booking.UserId);

            var usableWallet =
            wallet?.WalletStatus=="ACTIVE"
            ? Math.Max(0,wallet.WalletBalance-wallet.BlockedBalance)
            : 0;

            var appliedAmount =
            Math.Min(
            Math.Max(0,walletAmount),
            Math.Min(usableWallet,booking.TotalAmount));

            HttpContext.Session.SetString(
            GetWalletUsageSessionKey(booking.Id),
            appliedAmount.ToString(System.Globalization.CultureInfo.InvariantCulture));

            TempData["Success"] =
            appliedAmount>0
            ? $"Wallet amount applied: {CurrencyFormatter.FormatRupees(appliedAmount)}"
            : "Wallet amount removed.";

            return RedirectToAction(
            "Details",
            new { id=booking.Id });
        }
// ==========================================
// MY BOOKINGS
// ==========================================

public async Task<IActionResult> MyBookings()
{
    var userIdText =
    HttpContext.Session.GetString("UserId");

    if(!long.TryParse(userIdText,out var userId))
    {
        return RedirectToAction("Login","Auth");
    }

    var now = DateTime.UtcNow;
    var bookings=

    await _context
    .VwBookingCompleteDetails
    .AsNoTracking()
    .Where(x=>x.UserId==userId && x.StartTime>=now)
    .OrderBy(
    x=>x.StartTime)
    .ThenByDescending(
    x=>x.BookedAt)
    .ToListAsync();

    var bookingIds = bookings.Select(x=>x.BookingId).ToList();

    var bookingVenueRows =
    (
        await
        (
            from booking in _context.Bookings.AsNoTracking()
            join schedule in _context.ShowSchedules.AsNoTracking()
                on booking.ScheduleId equals schedule.Id
            join screen in _context.Screens.AsNoTracking()
                on schedule.ScreenId equals screen.Id into screenGroup
            from screen in screenGroup.DefaultIfEmpty()
            join venue in _context.Venues.AsNoTracking()
                on screen.VenueId equals venue.Id into venueGroup
            from venue in venueGroup.DefaultIfEmpty()
            where bookingIds.Contains(booking.Id)
            select new
            {
                booking.Id,
                VenueName = venue!=null ? venue.VenueName : null,
                ScreenName = screen!=null ? screen.ScreenName : null,
                Address = venue!=null ? venue.Address : null,
                City = venue!=null ? venue.City : null
            }
        ).ToListAsync()
    );

    var venueLookup =
    bookingVenueRows.ToDictionary(
        x=>x.Id,
        x=>string.Join(" / ",new[] { x.VenueName, x.ScreenName, x.Address, x.City }
            .Where(value=>!string.IsNullOrWhiteSpace(value))));

    ViewBag.BookingVenues=venueLookup;
    ViewBag.BookingScreens=
    bookingVenueRows.ToDictionary(
        x=>x.Id,
        x=>string.IsNullOrWhiteSpace(x.ScreenName) ? "Screen TBA" : x.ScreenName);

    ViewBag.PublicBaseUrl=
    GetPublicBaseUrl();

    return View(
    bookings);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CancelBooking(long bookingId, string? reason)
{
    var userIdText = HttpContext.Session.GetString("UserId");

    if(!long.TryParse(userIdText,out var userId))
    {
        return RedirectToAction("Login","Auth");
    }

    var booking = await _context.Bookings.FirstOrDefaultAsync(x=>x.Id==bookingId && x.UserId==userId);
    if(booking==null)
    {
        TempData["Error"]="Booking not found.";
        return RedirectToAction(nameof(MyBookings));
    }

    var schedule = await _context.ShowSchedules.FirstOrDefaultAsync(x=>x.Id==booking.ScheduleId);
    var now = DateTime.UtcNow;

    if(schedule==null || schedule.StartTime<=DateTime.UtcNow || booking.BookingStatus=="CANCELLED")
    {
        TempData["Error"]="Cancellation is not allowed for this booking.";
        return RedirectToAction(nameof(MyBookings));
    }

    if(booking.BookingStatus!="CONFIRMED" && booking.PaymentStatus!="SUCCESS")
    {
        TempData["Error"]="Only confirmed paid bookings can be cancelled.";
        return RedirectToAction(nameof(MyBookings));
    }

    var transaction = await _context.Transactions
    .Where(x=>x.BookingId==booking.Id || x.Id==booking.TransactionId)
    .OrderByDescending(x=>x.CompletedAt)
    .ThenByDescending(x=>x.CreatedAt)
    .FirstOrDefaultAsync();

    if(transaction==null)
    {
        TempData["Error"]="Payment transaction was not found for this booking.";
        return RedirectToAction(nameof(MyBookings));
    }

    await using var dbTransaction = await _context.Database.BeginTransactionAsync();

    booking.BookingStatus="CANCELLED";
    booking.PaymentStatus="REFUND_PENDING";
    booking.RefundStatus="PENDING";
    booking.CancelledAt=DateTime.UtcNow;
    booking.CancellationReason=string.IsNullOrWhiteSpace(reason) ? "Cancelled by customer" : reason.Trim();
    booking.UpdatedAt=DateTime.UtcNow;

    var refundAmount = Math.Max(0,booking.PayableAmount ?? booking.TotalAmount);
    var walletAmount = Math.Max(0,booking.WalletAmountUsed ?? 0);
    var couponAmount = Math.Max(0,booking.DiscountAmount ?? 0);
    var refundMethod = ResolveRefundMethod(transaction.PaymentMethod, walletAmount, refundAmount);
    var refundStatus = ShouldAutoRefund(refundMethod) ? "SUCCESS" : "PENDING";

    var refund = new Refund
    {
        booking_id=booking.Id,
        transaction_id=transaction.Id,
        user_id=booking.UserId,
        refund_ref=$"RFD-{booking.Id}-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
        refund_amount=refundAmount + walletAmount,
        refund_reason=booking.CancellationReason,
        refund_status=refundStatus,
        refund_method=refundMethod,
        gateway_refund_id=refundStatus=="SUCCESS" ? $"AUTO-{Guid.NewGuid():N}" : null,
        requested_at=DateTime.UtcNow,
        processed_at=refundStatus=="SUCCESS" ? DateTime.UtcNow : null,
        created_at=DateTime.UtcNow,
        updated_at=DateTime.UtcNow,
        workflow_action=refundStatus=="SUCCESS" ? "AUTO_REFUNDED" : "CUSTOMER_CANCELLED",
        admin_notes=refundStatus=="SUCCESS"
            ? $"Refund completed automatically. Coupon discount excluded: {couponAmount:0.00}."
            : $"Refund case raised for admin approval. Coupon discount excluded: {couponAmount:0.00}."
    };

    _context.Refunds.Add(refund);
    await _context.SaveChangesAsync();

    if(walletAmount>0)
    {
        await CreditWalletRefund(booking,transaction,refund,walletAmount);
    }

    if(booking.CouponId.HasValue && couponAmount>0)
    {
        await RecordCouponReversal(
        booking,
        transaction,
        booking.CouponId.Value,
        couponAmount);
    }

    transaction.RefundStatus=refundStatus;
    transaction.RefundedAmount=(transaction.RefundedAmount ?? 0) + refund.refund_amount;
    transaction.UpdatedAt=DateTime.UtcNow;

    var tickets = await _context.Tickets.Where(x=>x.BookingId==booking.Id).ToListAsync();
    foreach(var ticket in tickets)
    {
        ticket.TicketStatus="CANCELLED";
        ticket.UpdatedAt=DateTime.UtcNow;
    }

    var bookingSeats = await _context.BookingSeats.Where(x=>x.BookingId==booking.Id).ToListAsync();
    foreach(var seat in bookingSeats)
    {
        seat.BookingStatus="CANCELLED";
    }

    var seatIds = bookingSeats.Select(x=>x.ScreenSeatId).ToList();
    var locks = await _context.SeatLocks
    .Where(x=>x.ScheduleId==booking.ScheduleId && seatIds.Contains(x.ScreenSeatId))
    .ToListAsync();

    foreach(var seatLock in locks)
    {
        seatLock.LockStatus="RELEASED";
    }

    await _context.SaveChangesAsync();
    await dbTransaction.CommitAsync();

    TempData["Success"] = refundStatus=="SUCCESS"
        ? "Booking cancelled and refund processed."
        : "Booking cancelled. Refund case has been raised for admin approval.";

    return RedirectToAction(nameof(MyBookings));
}



        // ==========================================
        // GENERATE QR
        // ==========================================

        public async Task<IActionResult>
        GenerateQR(long bookingId)
        {
            await EnsurePaymentSessionDraftCompatibility();

            string token=
            Guid.NewGuid().ToString();

            var session=
            new PaymentSession
            {
                BookingId=bookingId,

                SessionToken=token,

                Status="PENDING",

                ExpiresAt=
                DateTime.UtcNow
                .AddMinutes(5)
            };

            _context.PaymentSessions
            .Add(session);

            await _context.SaveChangesAsync();

            var url=
            BuildAbsoluteUrl($"/Booking/MobilePay?token={Uri.EscapeDataString(token)}");
            return Json(new
            {
                success=true,
                url
            });
        }

        private async Task EnsurePaymentSessionDraftCompatibility()
        {
            await _context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE public.""PaymentSessions""
DROP CONSTRAINT IF EXISTS fk_payment_session_booking;");
        }



        // ==========================================
        // CREATE QR IMAGE
        // ==========================================

        public IActionResult
        CreateQR(string text)
        {
            QRCodeGenerator generator=
            new QRCodeGenerator();

            QRCodeData data=
            generator.CreateQrCode(
            text,
            QRCodeGenerator.ECCLevel.Q);

            PngByteQRCode qr=
            new PngByteQRCode(data);

            byte[] bytes=
            qr.GetGraphic(20);

            return File(
            bytes,
            "image/png");
        }



        // ==========================================
        // MOBILE PAYMENT PAGE
        // ==========================================

        // public async Task<IActionResult>
        // MobilePay(string token)
        // {
        //     var session=

        //     await _context
        //     .PaymentSessions
        //     .FirstOrDefaultAsync(
        //     x=>x.SessionToken==token);

        //     if(session==null)
        //         return NotFound();

        //     var booking=

        //     await _context
        //     .BookingDrafts
        //     .FindAsync(
        //     session.BookingId);

        //     return View(
        //     booking);
        // }
public async Task<IActionResult>
MobilePay(string token)
{
    var session=

    await _context
    .PaymentSessions
    .FirstOrDefaultAsync(
    x=>x.SessionToken==token);

    if(session==null)
        return NotFound();

    if(session.ExpiresAt<DateTime.UtcNow ||
       !string.Equals(session.Status,"PENDING",StringComparison.OrdinalIgnoreCase))
    {
        return View("MobilePayExpired");
    }


    var booking=

    await _context
    .BookingDrafts
    .FindAsync(
    session.BookingId);

    if(booking==null)
        return NotFound();


    var schedule=

    await _context
    .ShowSchedules
    .Include(x=>x.Movie)
    .Include(x=>x.StandupShow)
    .Include(x=>x.LiveStream)
    .Include(x=>x.Location)
    .Include(x=>x.Screen)
    .FirstOrDefaultAsync(
    x=>x.Id==
    booking.ScheduleId);


    var user=

    await _context
    .Users
    .FirstOrDefaultAsync(
    x=>x.Id==
    booking.UserId);


    ViewBag.Schedule=
    schedule;
    await SetTheaterViewBag(schedule);

    ViewBag.User=
    user;

    return View(
    booking);
}

private string GetWalletUsageSessionKey(long bookingId)
{
    return $"WalletUse_{bookingId}";
}

private string GetCouponDiscountSessionKey(long bookingId)
{
    return $"CouponDiscount_{bookingId}";
}

private string GetCouponCodeSessionKey(long bookingId)
{
    return $"CouponCode_{bookingId}";
}

private string GetCouponIdSessionKey(long bookingId)
{
    return $"CouponId_{bookingId}";
}

private decimal GetWalletUsage(long bookingId)
{
    var value =
    HttpContext.Session.GetString(
    GetWalletUsageSessionKey(bookingId));

    return decimal.TryParse(
    value,
    System.Globalization.NumberStyles.Number,
    System.Globalization.CultureInfo.InvariantCulture,
    out var amount)
    ? Math.Max(0,amount)
    : 0;
}

private decimal GetCouponDiscount(long bookingId)
{
    var value =
    HttpContext.Session.GetString(
    GetCouponDiscountSessionKey(bookingId));

    return decimal.TryParse(
    value,
    System.Globalization.NumberStyles.Number,
    System.Globalization.CultureInfo.InvariantCulture,
    out var amount)
    ? Math.Max(0,amount)
    : 0;
}

private long? GetCouponId(long bookingId)
{
    var value =
    HttpContext.Session.GetString(
    GetCouponIdSessionKey(bookingId));

    return long.TryParse(value,out var couponId)
    ? couponId
    : null;
}

private async Task<List<string>> GetActiveCouponSummaries()
{
    var coupons =
    new List<string>();

    var connection =
    _context.Database.GetDbConnection();

    if(connection.State!=System.Data.ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    await using var command =
    connection.CreateCommand();

    command.CommandText = @"
SELECT coupon_code, discount_type, discount_value
FROM coupons
WHERE coupon_status='ACTIVE'
  AND valid_from <= CURRENT_TIMESTAMP
  AND valid_to >= CURRENT_TIMESTAMP
ORDER BY id
LIMIT 6;";

    await using var reader =
    await command.ExecuteReaderAsync();

    while(await reader.ReadAsync())
    {
        var code=reader.GetString(0);
        var type=reader.GetString(1);
        var value=reader.GetDecimal(2);
        var label=type=="PERCENTAGE" ? $"{value:0}% off" : CurrencyFormatter.FormatRupees(value);
        coupons.Add($"{code} - {label}");
    }

    return coupons;
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ApplyCoupon(
long bookingId,
string couponCode)
{
    var booking =
    await _context.BookingDrafts
    .FirstOrDefaultAsync(x=>x.Id==bookingId);

    if(booking==null)
    {
        return NotFound();
    }

    if(string.IsNullOrWhiteSpace(couponCode))
    {
        HttpContext.Session.Remove(GetCouponDiscountSessionKey(bookingId));
        HttpContext.Session.Remove(GetCouponCodeSessionKey(bookingId));
        HttpContext.Session.Remove(GetCouponIdSessionKey(bookingId));
        TempData["Success"]="Coupon removed.";
        return RedirectToAction("Details",new { id=bookingId });
    }

    var schedule =
    await _context.ShowSchedules
    .AsNoTracking()
    .FirstOrDefaultAsync(x=>x.Id==booking.ScheduleId);

    var connection =
    _context.Database.GetDbConnection();

    if(connection.State!=System.Data.ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    await using var command =
    connection.CreateCommand();

    command.CommandText = @"
SELECT
    c.id,
    c.coupon_code,
    c.discount_type,
    c.discount_value,
    COALESCE(c.minimum_booking_amount,0) AS minimum_booking_amount,
    c.maximum_discount_amount,
    c.usage_limit,
    COALESCE(c.usage_per_user,1) AS usage_per_user,
    COALESCE(c.used_count,0) AS used_count,
    c.valid_from,
    c.valid_to,
    c.coupon_status,
    c.applicable_show_type,
    (
        SELECT count(*)
        FROM coupon_usage cu
        WHERE cu.coupon_id=c.id
          AND cu.user_id=@user_id
          AND cu.usage_status='SUCCESS'
    ) AS user_used_count
FROM coupons c
WHERE lower(c.coupon_code)=lower(@coupon_code)
LIMIT 1;";

    var codeParameter =
    command.CreateParameter();
    codeParameter.ParameterName="@coupon_code";
    codeParameter.Value=couponCode.Trim();
    command.Parameters.Add(codeParameter);

    var userParameter =
    command.CreateParameter();
    userParameter.ParameterName="@user_id";
    userParameter.Value=booking.UserId;
    command.Parameters.Add(userParameter);

    await using var reader =
    await command.ExecuteReaderAsync();

    if(!await reader.ReadAsync())
    {
        TempData["Error"]="Coupon code was not found.";
        return RedirectToAction("Details",new { id=bookingId });
    }

    var couponId=reader.GetInt64(0);
    var dbCode=reader.GetString(1);
    var discountType=reader.GetString(2);
    var discountValue=reader.GetDecimal(3);
    var minimumAmount=reader.GetDecimal(4);
    var maxDiscount=reader.IsDBNull(5) ? (decimal?)null : reader.GetDecimal(5);
    var usageLimit=reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6);
    var usagePerUser=reader.GetInt32(7);
    var usedCount=reader.GetInt32(8);
    var validFrom=reader.GetDateTime(9);
    var validTo=reader.GetDateTime(10);
    var status=reader.GetString(11);
    var applicableShowType=reader.IsDBNull(12) ? null : reader.GetString(12);
    var userUsedCount=reader.GetInt64(13);

var now = DateTime.UtcNow;

    if(status!="ACTIVE" || validFrom>now || validTo<now)
    {
        TempData["Error"]="Coupon is not active.";
        return RedirectToAction("Details",new { id=bookingId });
    }

    if(booking.TotalAmount<minimumAmount)
    {
        TempData["Error"]=$"Coupon requires minimum booking amount {CurrencyFormatter.FormatRupees(minimumAmount)}.";
        return RedirectToAction("Details",new { id=bookingId });
    }

    if(usageLimit.HasValue && usedCount>=usageLimit.Value)
    {
        TempData["Error"]="Coupon usage limit is over.";
        return RedirectToAction("Details",new { id=bookingId });
    }

    if(userUsedCount>=usagePerUser)
    {
        TempData["Error"]="You have already used this coupon.";
        return RedirectToAction("Details",new { id=bookingId });
    }

    if(!string.IsNullOrWhiteSpace(applicableShowType)
        && !string.Equals(applicableShowType,schedule?.Type,StringComparison.OrdinalIgnoreCase))
    {
        TempData["Error"]="Coupon is not valid for this show type.";
        return RedirectToAction("Details",new { id=bookingId });
    }

    var discount =
    discountType switch
    {
        "PERCENTAGE" => booking.TotalAmount * discountValue / 100,
        "FLAT" => discountValue,
        "CASHBACK" => 0,
        _ => 0
    };

    if(maxDiscount.HasValue)
    {
        discount=Math.Min(discount,maxDiscount.Value);
    }

    discount=Math.Min(Math.Max(0,discount),booking.TotalAmount);

    HttpContext.Session.SetString(
    GetCouponDiscountSessionKey(bookingId),
    discount.ToString(System.Globalization.CultureInfo.InvariantCulture));

    HttpContext.Session.SetString(
    GetCouponCodeSessionKey(bookingId),
    dbCode);

    HttpContext.Session.SetString(
    GetCouponIdSessionKey(bookingId),
    couponId.ToString());

    TempData["Success"]=
    $"Coupon {dbCode} applied: {CurrencyFormatter.FormatRupees(discount)} off.";

    return RedirectToAction("Details",new { id=bookingId });
}

private async Task GenerateSeats(
int scheduleId,
string type)
{
    bool exists=

    await _context.ScreenSeats
    .AnyAsync(
    x=>x.ScheduleId==scheduleId);

    if(exists)
        return;


    var screen=

    await _context.Screens
    .Where(
    x=>x.IsActive)
    .FirstOrDefaultAsync();

    if(screen==null)
    {
        throw new Exception(
        "No active screen found");
    }


    var rows=
    new List<string>
    {
        "A","B","C",
        "D","E","F","G"
    };

    var seats=
    new List<ScreenSeat>();


    foreach(var row in rows)
    {
        for(int i=1;i<=10;i++)
        {
            string category;
            decimal price;


            if(row=="A" || row=="B")
            {
                category="Premium";
                price=350;
            }
            else if(
            row=="C"
            ||
            row=="D"
            ||
            row=="E")
            {
                category="Gold";
                price=250;
            }
            else
            {
                category="Silver";
                price=150;
            }


            seats.Add(
            new ScreenSeat
            {
                ScheduleId=
                scheduleId,

                ScreenId=
                screen.Id,

                SeatRow=
                row,

                SeatNumber=
                i.ToString(),

                SeatCategory=
                category,

                SeatPrice=
                price,

                IsActive=
                true
            });
        }
    }

    await _context
    .ScreenSeats
    .AddRangeAsync(seats);

    await _context
    .SaveChangesAsync();
}

private async Task<Booking> FinalizeBookingPayment(
BookingDraft draft,
string paymentMethod,
decimal walletAmountUsed,
decimal couponDiscount,
long? couponId)
{
    couponDiscount =
    Math.Min(
    Math.Max(0,couponDiscount),
    draft.TotalAmount);

    walletAmountUsed =
    Math.Min(
    Math.Max(0,walletAmountUsed),
    Math.Max(0,draft.TotalAmount-couponDiscount));

    var payableAmount =
    Math.Max(0,draft.TotalAmount-couponDiscount-walletAmountUsed);

    if(walletAmountUsed>0)
    {
        var wallet =
        await _context.VwWalletSummaries
        .AsNoTracking()
        .FirstOrDefaultAsync(x=>x.UserId==draft.UserId);

        var usableWallet =
        wallet?.WalletStatus=="ACTIVE"
        ? Math.Max(0,wallet.WalletBalance-wallet.BlockedBalance)
        : 0;

        if(usableWallet<walletAmountUsed)
        {
            throw new InvalidOperationException(
            "Wallet balance is not available for this booking.");
        }
    }

    var existingBooking =
    await _context.Bookings
    .FirstOrDefaultAsync(
    x=>x.BookingRef==$"BKG-DRAFT-{draft.Id}");

    if(existingBooking!=null)
    {
        var existingTransaction =
        await _context.Transactions
        .Where(x=>x.BookingId==existingBooking.Id || x.Id==existingBooking.TransactionId)
        .OrderByDescending(x=>x.CompletedAt)
        .ThenByDescending(x=>x.CreatedAt)
        .FirstOrDefaultAsync();

        if(existingTransaction==null)
        {
            var nowForMissingTransaction =
DateTime.UtcNow;

            existingTransaction =
            new Transaction
            {
                TransactionRef=$"TXN-{draft.Id}-{nowForMissingTransaction:yyyyMMddHHmmssfff}",
                UserId=draft.UserId,
                TransactionType="BOOKING",
                PaymentMethod=paymentMethod,
                Amount=payableAmount>0 ? payableAmount : draft.TotalAmount,
                Currency="INR",
                Status="SUCCESS",
                GatewayName=paymentMethod=="QR" ? "QR" : "DUMMY_GATEWAY",
                GatewayTransactionId=Guid.NewGuid().ToString("N"),
                BookingId=existingBooking.Id,
                Description="Ticket booking payment",
                IpAddress=HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent=Request.Headers.UserAgent.ToString(),
                InitiatedAt=nowForMissingTransaction,
                CompletedAt=nowForMissingTransaction,
                CreatedAt=nowForMissingTransaction,
                UpdatedAt=nowForMissingTransaction,
                GatewayStatusCode="SUCCESS",
                RefundedAmount=0,
                ReconciliationStatus="PENDING",
                PaymentSource="WEB",
                RetryCount=0,
                IsDeleted=false
            };

            _context.Transactions.Add(existingTransaction);
            await _context.SaveChangesAsync();
        }

        existingBooking.TransactionId=existingTransaction.Id;
        existingBooking.PaymentStatus="SUCCESS";
        existingBooking.BookingStatus="CONFIRMED";
        existingBooking.OriginalAmount=draft.TotalAmount;
        existingBooking.DiscountAmount=couponDiscount;
        existingBooking.PayableAmount=payableAmount;
        existingBooking.WalletAmountUsed=walletAmountUsed;
        existingBooking.CouponId=couponId;
        var repairedAt =
        DateTime.UtcNow;

        existingBooking.UpdatedAt=repairedAt;
        existingBooking.ConfirmedAt ??= repairedAt;

        await _context.SaveChangesAsync();

        await DebitWalletForBooking(
        draft,
        existingBooking,
        existingTransaction,
        walletAmountUsed);

        await RecordCouponUsage(
        draft,
        existingBooking,
        existingTransaction,
        couponId,
        couponDiscount);

        return existingBooking;
    }

    await using var dbTransaction =
    await _context.Database.BeginTransactionAsync();

    var now =
    DateTime.UtcNow;

    var seatLabels =
    draft.SeatNumbers
    .Split(',',StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .ToList();

    var seats =
    await _context.ScreenSeats
    .Where(x=>x.ScheduleId==draft.ScheduleId)
    .ToListAsync();

    var selectedSeats =
    seats
    .Where(x=>seatLabels.Contains($"{x.SeatRow}{x.SeatNumber}"))
    .ToList();

    if(!selectedSeats.Any())
    {
        throw new InvalidOperationException(
        "No locked seats found for this booking.");
    }

    var user =
    await _context.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(x=>x.Id==draft.UserId);

    var booking =
    new Booking
    {
        BookingRef=$"BKG-DRAFT-{draft.Id}",
        UserId=draft.UserId,
        ScheduleId=draft.ScheduleId,
        BookingStatus="CONFIRMED",
        TotalAmount=draft.TotalAmount,
        TotalTickets=selectedSeats.Count,
        BookingSource="WEB",
        BookedAt=now,
        CreatedAt=now,
        UpdatedAt=now,
        CreatedBy=user?.Email,
        UpdatedBy=user?.Email,
        IsDeleted=false,
        OriginalAmount=draft.TotalAmount,
        DiscountAmount=couponDiscount,
        PayableAmount=payableAmount,
        TaxAmount=0,
        ConvenienceFee=0,
        WalletAmountUsed=walletAmountUsed,
        CouponId=couponId,
        PaymentStatus="SUCCESS",
        ConfirmedAt=now
    };

    _context.Bookings.Add(booking);
    await _context.SaveChangesAsync();

    var transaction =
    new Transaction
    {
        TransactionRef=$"TXN-{draft.Id}-{now:yyyyMMddHHmmssfff}",
        UserId=draft.UserId,
        TransactionType="BOOKING",
        PaymentMethod=paymentMethod,
        Amount=payableAmount>0 ? payableAmount : draft.TotalAmount,
        Currency="INR",
        Status="SUCCESS",
        GatewayName=paymentMethod=="QR" ? "QR" : "DUMMY_GATEWAY",
        GatewayTransactionId=Guid.NewGuid().ToString("N"),
        BookingId=booking.Id,
        Description="Ticket booking payment",
        IpAddress=HttpContext.Connection.RemoteIpAddress?.ToString(),
        UserAgent=Request.Headers.UserAgent.ToString(),
        InitiatedAt=now,
        CompletedAt=now,
        CreatedAt=now,
        UpdatedAt=now,
        GatewayStatusCode="SUCCESS",
        RefundedAmount=0,
        ReconciliationStatus="PENDING",
        PaymentSource="WEB",
        RetryCount=0,
        IsDeleted=false
    };

    _context.Transactions.Add(transaction);
    await _context.SaveChangesAsync();

    booking.TransactionId=transaction.Id;
    booking.UpdatedAt=now;

    await DebitWalletForBooking(
    draft,
    booking,
    transaction,
    walletAmountUsed);

    await RecordCouponUsage(
    draft,
    booking,
    transaction,
    couponId,
    couponDiscount);

    var bookingItem =
    new BookingItem
    {
        BookingId=booking.Id,
        TicketType="SEAT",
        Quantity=selectedSeats.Count,
        UnitPrice=selectedSeats.Count==0 ? draft.TotalAmount : draft.TotalAmount / selectedSeats.Count,
        TotalPrice=draft.TotalAmount,
        AttendeeName=user?.Name,
        AttendeeMobile=user?.Mobile,
        AttendeeEmail=user?.Email,
        CreatedAt=now
    };

    _context.BookingItems.Add(bookingItem);
    await _context.SaveChangesAsync();

    foreach(var seat in selectedSeats)
    {
        var seatNumber =
        $"{seat.SeatRow}{seat.SeatNumber}";

        var ticket =
        new Ticket
        {
            BookingId=booking.Id,
            TicketNumber=$"TKT-{booking.Id}-{seatNumber}-{Guid.NewGuid():N}".Substring(0,32),
            AttendeeName=user?.Name,
            SeatNumber=seatNumber,
            QrCode=$"BOOKING:{booking.BookingRef};SEAT:{seatNumber}",
            TicketStatus="ACTIVE",
            IssuedAt=now,
            CreatedAt=now,
            UpdatedAt=now,
            QrGeneratedAt=now,
            ValidationStatus="NOT_SCANNED"
        };

        _context.Tickets.Add(ticket);

        _context.BookingSeats.Add(
        new BookingSeat
        {
            BookingId=booking.Id,
            ScreenSeatId=seat.Id,
            BookingItemId=bookingItem.Id,
            SeatPrice=seat.SeatPrice,
            BookingStatus="BOOKED",
            QrCode=$"BOOKING:{booking.BookingRef};SEAT:{seatNumber}",
            CreatedAt=now
        });
    }

    var locks =
    await _context.SeatLocks
    .Where(x=>
        x.UserId==draft.UserId
        &&
        x.ScheduleId==draft.ScheduleId
        &&
        selectedSeats.Select(s=>s.Id).Contains(x.ScreenSeatId)
        &&
        x.LockStatus=="LOCKED")
    .ToListAsync();

    foreach(var seatLock in locks)
    {
        seatLock.LockStatus="CONFIRMED";
    }

    await _context.SaveChangesAsync();

    await dbTransaction.CommitAsync();

    return booking;
}

private async Task DebitWalletForBooking(
BookingDraft draft,
Booking booking,
Transaction transaction,
decimal walletAmountUsed)
{
    if(walletAmountUsed<=0)
    {
        return;
    }

    var reference =
    $"WLT-{booking.Id}-{transaction.Id}";

    var insertedRows =
    await _context.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO wallet_transactions
(
    wallet_id,
    user_id,
    booking_id,
    transaction_id,
    transaction_ref,
    transaction_type,
    entry_type,
    amount,
    opening_balance,
    closing_balance,
    remarks,
    transaction_status,
    created_at,
    created_by,
    description,
    status,
    reference_type,
    reference_id,
    balance_before,
    balance_after,
    payment_method,
    gateway_name,
    gateway_reference,
    is_deleted
)
SELECT
    uw.id,
    {draft.UserId},
    {booking.Id},
    {transaction.Id},
    {reference},
    'BOOKING',
    'DEBIT',
    {walletAmountUsed},
    uw.wallet_balance,
    uw.wallet_balance - {walletAmountUsed},
    'Wallet used for ticket booking',
    'SUCCESS',
    CURRENT_TIMESTAMP,
    {draft.UserId.ToString()},
    'Wallet debit for booking payment',
    'SUCCESS',
    'BOOKING',
    {booking.Id},
    uw.wallet_balance,
    uw.wallet_balance - {walletAmountUsed},
    'WALLET',
    'WALLET',
    {transaction.TransactionRef ?? reference},
    false
FROM user_wallets uw
WHERE uw.user_id = {draft.UserId}
  AND uw.wallet_status = 'ACTIVE'
  AND uw.wallet_balance >= {walletAmountUsed}
  AND NOT EXISTS
  (
      SELECT 1
      FROM wallet_transactions wt
      WHERE wt.booking_id = {booking.Id}
        AND wt.transaction_type = 'BOOKING'
        AND wt.entry_type = 'DEBIT'
        AND wt.is_deleted = false
  );");

    _ = insertedRows;
}

private async Task RecordCouponUsage(
BookingDraft draft,
Booking booking,
Transaction transaction,
long? couponId,
decimal couponDiscount)
{
    if(!couponId.HasValue || couponDiscount<=0)
    {
        return;
    }

    await _context.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO coupon_usage
(
    coupon_id,
    booking_id,
    transaction_id,
    user_id,
    coupon_code,
    original_amount,
    discount_amount,
    final_amount,
    usage_status,
    used_at
)
SELECT
    c.id,
    {booking.Id},
    {transaction.Id},
    {draft.UserId},
    c.coupon_code,
    {draft.TotalAmount},
    {couponDiscount},
    {Math.Max(0,draft.TotalAmount-couponDiscount-booking.WalletAmountUsed.GetValueOrDefault())},
    'SUCCESS',
    CURRENT_TIMESTAMP
FROM coupons c
WHERE c.id = {couponId.Value}
  AND NOT EXISTS
  (
      SELECT 1
      FROM coupon_usage cu
      WHERE cu.booking_id = {booking.Id}
        AND cu.coupon_id = c.id
        AND cu.usage_status = 'SUCCESS'
  );

UPDATE coupons c
SET used_count =
    (
        SELECT count(*)
        FROM coupon_usage cu
        WHERE cu.coupon_id = c.id
          AND cu.usage_status = 'SUCCESS'
    ),
    updated_at = CURRENT_TIMESTAMP
WHERE c.id = {couponId.Value};");
}

private async Task RecordCouponReversal(
Booking booking,
Transaction transaction,
long couponId,
decimal couponDiscount)
{
    if(couponDiscount<=0)
    {
        return;
    }

    await _context.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO coupon_usage
(
    coupon_id,
    booking_id,
    transaction_id,
    user_id,
    coupon_code,
    original_amount,
    discount_amount,
    final_amount,
    usage_status,
    used_at
)
SELECT
    c.id,
    {booking.Id},
    {transaction.Id},
    {booking.UserId},
    c.coupon_code,
    {Math.Max(0,booking.OriginalAmount ?? booking.TotalAmount)},
    {-couponDiscount},
    {-Math.Max(0,booking.PayableAmount ?? booking.TotalAmount)},
    'REVERSED',
    CURRENT_TIMESTAMP
FROM coupons c
WHERE c.id = {couponId}
  AND NOT EXISTS
  (
      SELECT 1
      FROM coupon_usage cu
      WHERE cu.booking_id = {booking.Id}
        AND cu.coupon_id = c.id
        AND cu.usage_status = 'REVERSED'
  );

UPDATE coupons c
SET used_count =
    GREATEST(
        0,
        (
            SELECT count(*)
            FROM coupon_usage cu
            WHERE cu.coupon_id = c.id
              AND cu.usage_status = 'SUCCESS'
        )
        -
        (
            SELECT count(*)
            FROM coupon_usage cu
            WHERE cu.coupon_id = c.id
              AND cu.usage_status = 'REVERSED'
        )
    )::integer,
    updated_at = CURRENT_TIMESTAMP
WHERE c.id = {couponId};");
}
// ==========================================
// APPROVE QR PAYMENT
// ==========================================

[HttpPost]
public async Task<IActionResult>
ApprovePayment(string token)
{
    var session =

    await _context
    .PaymentSessions
    .FirstOrDefaultAsync(
    x=>x.SessionToken==token);

    if(session==null)
    {
        return Json(new
        {
            success=false,
            message="Payment session was not found."
        });
    }

    if(session.ExpiresAt<DateTime.UtcNow ||
       !string.Equals(session.Status,"PENDING",StringComparison.OrdinalIgnoreCase))
    {
        return Json(new
        {
            success=false,
            message="Payment session has expired or was already completed."
        });
    }

    var booking=

    await _context
    .BookingDrafts
    .FindAsync(
    session.BookingId);

    if(booking==null)
    {
        return Json(new
        {
            success=false,
            message="Booking draft was not found."
        });
    }

    booking.Status=
    "CONFIRMED";

    session.Status="SUCCESS";

    try
    {
        var confirmedBooking =
        await FinalizeBookingPayment(
        booking,
        "QR",
        GetWalletUsage(booking.Id),
        GetCouponDiscount(booking.Id),
        GetCouponId(booking.Id));

        _context.BookingTransactions.Add(

        new BookingTransaction
        {
            BookingId=
            confirmedBooking.Id,

            TransactionRef=
            Guid.NewGuid().ToString(),

            PaymentMethod=
            "QR",

            Amount=
            booking.TotalAmount,

            PaymentStatus=
            "SUCCESS",

            CreatedAt=
            DateTime.UtcNow,

            PaidAt=
            DateTime.UtcNow
        });
    }
    catch(Exception ex)
    {
        return Json(new
        {
            success=false,
            message=ex.Message
        });
    }

    await _context.SaveChangesAsync();

    HttpContext.Session.Remove(
    GetWalletUsageSessionKey(booking.Id));
    HttpContext.Session.Remove(
    GetCouponDiscountSessionKey(booking.Id));
    HttpContext.Session.Remove(
    GetCouponCodeSessionKey(booking.Id));
    HttpContext.Session.Remove(
    GetCouponIdSessionKey(booking.Id));

    return Json(new
    {
        success=true
    });
}


// ==========================================
// REJECT QR PAYMENT
// ==========================================
[HttpPost]
public async Task<IActionResult>
RejectPayment(string token)
{
    var session=

    await _context
    .PaymentSessions
    .FirstOrDefaultAsync(
    x=>x.SessionToken==token);

    if(session==null)
    {
        return Json(new
        {
            success=false,
            message="Payment session was not found."
        });
    }

    if(session.ExpiresAt<DateTime.UtcNow ||
       !string.Equals(session.Status,"PENDING",StringComparison.OrdinalIgnoreCase))
    {
        return Json(new
        {
            success=false,
            message="Payment session has expired or was already completed."
        });
    }

    session.Status=
    "FAILED";

    var booking=

    await _context
    .BookingDrafts
    .FindAsync(
    session.BookingId);

    if(booking!=null)
    {
        booking.Status=
        "FAILED";

        var seatLabels =
        booking.SeatNumbers
        .Split(',',StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();

        var seats =
        await _context.ScreenSeats
        .Where(x=>x.ScheduleId==booking.ScheduleId)
        .ToListAsync();

        var selectedSeatIds =
        seats
        .Where(x=>seatLabels.Contains($"{x.SeatRow}{x.SeatNumber}"))
        .Select(x=>x.Id)
        .ToList();

        var locks =
        await _context.SeatLocks
        .Where(x=>
            x.UserId==booking.UserId
            &&
            x.ScheduleId==booking.ScheduleId
            &&
            selectedSeatIds.Contains(x.ScreenSeatId)
            &&
            x.LockStatus=="LOCKED")
        .ToListAsync();

        foreach(var seatLock in locks)
        {
            seatLock.LockStatus="RELEASED";
        }
    }

    await _context
    .SaveChangesAsync();

    return Json(new
    {
        success=true
    });
}
// ==========================================
// CHECK PAYMENT STATUS
// ==========================================

[HttpGet]
public async Task<IActionResult>
CheckPaymentStatus(long bookingId)
{
    var session=

    await _context
    .PaymentSessions
    .Where(
    x=>x.BookingId==bookingId)
    .OrderByDescending(
    x=>x.Id)
    .FirstOrDefaultAsync();

    if(session==null)
    {
        return Json(new
        {
            status="PENDING"
        });
    }

    return Json(new
    {
        status=session.Status
    });
}

        // ==========================================
        // COMPLETE PAYMENT
        // ==========================================

        [HttpPost]
        public async Task<IActionResult>
        CompletePayment(
        [FromBody]
        PaymentRequest request)
        {
            var booking=

            await _context
            .BookingDrafts
            .FindAsync(
            request.BookingId);

            if(booking==null)
            {
                return Json(new
                {
                    success=false
                });
            }

            booking.Status=
            "CONFIRMED";

            try
            {
                var confirmedBooking =
                await FinalizeBookingPayment(
                booking,
                request.PaymentMethod,
                GetWalletUsage(booking.Id),
                GetCouponDiscount(booking.Id),
                GetCouponId(booking.Id));

                var transaction=
                new BookingTransaction
                {
                    BookingId=
                    confirmedBooking.Id,

                    TransactionRef=
                    Guid.NewGuid().ToString(),

                    PaymentMethod=
                    request.PaymentMethod,

                    Amount=
                    booking.TotalAmount,

                    PaymentStatus=
                    "SUCCESS",

                    CreatedAt=
                    DateTime.UtcNow,

                    PaidAt=
                    DateTime.UtcNow
                };

                _context
                .BookingTransactions
                .Add(transaction);
            }
            catch(Exception ex)
            {
                return Json(new
                {
                    success=false,
                    message=ex.Message
                });
            }

            booking.Status=
            "CONFIRMED";

            await _context
            .SaveChangesAsync();

            HttpContext.Session.Remove(
            GetWalletUsageSessionKey(booking.Id));
            HttpContext.Session.Remove(
            GetCouponDiscountSessionKey(booking.Id));
            HttpContext.Session.Remove(
            GetCouponCodeSessionKey(booking.Id));
            HttpContext.Session.Remove(
            GetCouponIdSessionKey(booking.Id));

            return Json(new
            {
                success=true
            });
        }
// ==========================================
// CONFIRMATION
// ==========================================

public async Task<IActionResult>
Confirmation(long bookingId, long? confirmedBookingId)
{
    BookingDraft? booking = null;
    Booking? confirmedBooking = null;

    if(confirmedBookingId.HasValue)
    {
        confirmedBooking =
        await _context.Bookings
        .AsNoTracking()
        .FirstOrDefaultAsync(x=>x.Id==confirmedBookingId.Value);

        if(confirmedBooking==null)
        {
            return NotFound();
        }

        var userIdText =
        HttpContext.Session.GetString("UserId");

        if(long.TryParse(userIdText,out var currentUserId)
            && currentUserId!=confirmedBooking.UserId
            && !User.IsInRole("Admin"))
        {
            return StatusCode(
            StatusCodes.Status403Forbidden,
            "You do not have access to this booking.");
        }

        var draftIdText =
        confirmedBooking.BookingRef?.StartsWith("BKG-DRAFT-",StringComparison.OrdinalIgnoreCase)==true
        ? confirmedBooking.BookingRef["BKG-DRAFT-".Length..]
        : string.Empty;

        if(long.TryParse(draftIdText,out var draftId))
        {
            booking =
            await _context
            .BookingDrafts
            .FirstOrDefaultAsync(x=>x.Id==draftId);
        }

        booking ??= new BookingDraft
        {
            Id=confirmedBooking.Id,
            UserId=confirmedBooking.UserId,
            ScheduleId=confirmedBooking.ScheduleId,
            SeatNumbers=string.Empty,
            TotalAmount=confirmedBooking.PayableAmount ?? confirmedBooking.TotalAmount,
            Status=confirmedBooking.BookingStatus,
            CreatedAt=confirmedBooking.BookedAt ?? confirmedBooking.CreatedAt ?? DateTime.UtcNow
        };
    }
    else
    {
        booking=
        await _context
        .BookingDrafts
        .FirstOrDefaultAsync(
        x=>x.Id==bookingId);
    }

    if(booking==null)
    {
        return NotFound();
    }

    var schedule=

    await _context
    .ShowSchedules
    .Include(x=>x.Movie)
    .Include(x=>x.StandupShow)
    .Include(x=>x.LiveStream)
    .Include(x=>x.Location)
    .Include(x=>x.Screen)
    .FirstOrDefaultAsync(
    x=>x.Id==
    booking.ScheduleId);

    ViewBag.Schedule=
    schedule;
    await SetTheaterViewBag(schedule);

    var user =
    await _context.Users
    .FirstOrDefaultAsync(x=>x.Id==booking.UserId);

    confirmedBooking ??=
    await _context.Bookings
    .AsNoTracking()
    .FirstOrDefaultAsync(x=>x.BookingRef==$"BKG-DRAFT-{booking.Id}");

    ViewBag.User=user;
    ViewBag.Email=user?.Email;
    ViewBag.Phone=user?.Mobile;
    ViewBag.TicketUrl=confirmedBooking==null
    ? BuildAbsoluteUrl($"/Booking/Ticket/{booking.Id}")
    : BuildAbsoluteUrl($"/Booking/TicketByBooking/{confirmedBooking.Id}");

    return View(
    booking);
}
[HttpGet]
public async Task<IActionResult>
CheckQRStatus(long bookingId)
{
    var session=

    await _context
    .PaymentSessions
    .Where(
    x=>x.BookingId==bookingId)
    .OrderByDescending(
    x=>x.Id)
    .FirstOrDefaultAsync();

    if(session==null)
    {
        return Json(new
        {
            status="PENDING"
        });
    }

    return Json(new
    {
        status=session.Status
    });
}

private async Task SetTheaterViewBag(ShowSchedule? schedule)
{
    Screen? screen = schedule?.Screen;

    if(screen==null && schedule?.ScreenId!=null)
    {
        screen = await _context.Screens
        .AsNoTracking()
        .FirstOrDefaultAsync(x=>x.Id==schedule.ScreenId.Value);
    }

    Venue? venue = null;

    if(screen!=null)
    {
        venue = await _context.Venues
        .AsNoTracking()
        .FirstOrDefaultAsync(x=>x.Id==screen.VenueId);
    }

    ViewBag.Screen=screen;
    ViewBag.Venue=venue;
}

private string ResolveRefundMethod(string? paymentMethod, decimal walletAmount, decimal sourceAmount)
{
    var method = (paymentMethod ?? string.Empty).Trim().ToUpperInvariant();

    if(walletAmount>0 && sourceAmount<=0)
    {
        return "WALLET";
    }

    if(method.Contains("UPI"))
    {
        return walletAmount>0 ? "WALLET_UPI" : "UPI";
    }

    if(method=="QR")
    {
        return walletAmount>0 ? "WALLET_QR" : "QR";
    }

    if(method.Contains("WALLET"))
    {
        return "WALLET";
    }

    if(method.Contains("CARD") || method.Contains("NET") || method.Contains("BANK"))
    {
        return method;
    }

    return walletAmount>0 ? "WALLET_SOURCE" : "SOURCE";
}

private bool ShouldAutoRefund(string refundMethod)
{
    var method = refundMethod.ToUpperInvariant();
    return method.Contains("WALLET") || method=="UPI" || method=="QR" || method=="SOURCE";
}

private async Task CreditWalletRefund(
Booking booking,
Transaction transaction,
Refund refund,
decimal amount)
{
    if(amount<=0)
    {
        return;
    }

    var reference = $"WLR-{booking.Id}-{refund.id}";

    await _context.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO wallet_transactions
(
    wallet_id,
    user_id,
    booking_id,
    transaction_id,
    refund_id,
    transaction_ref,
    transaction_type,
    entry_type,
    amount,
    opening_balance,
    closing_balance,
    remarks,
    transaction_status,
    created_at,
    created_by,
    description,
    status,
    reference_type,
    reference_id,
    balance_before,
    balance_after,
    payment_method,
    gateway_name,
    gateway_reference,
    is_deleted
)
SELECT
    uw.id,
    {booking.UserId},
    {booking.Id},
    {transaction.Id},
    {refund.id},
    {reference},
    'REFUND',
    'CREDIT',
    {amount},
    uw.wallet_balance,
    uw.wallet_balance + {amount},
    'Wallet refund for cancelled booking',
    'SUCCESS',
    CURRENT_TIMESTAMP,
    {booking.UserId.ToString()},
    'Wallet credit after booking cancellation',
    'SUCCESS',
    'REFUND',
    {refund.id},
    uw.wallet_balance,
    uw.wallet_balance + {amount},
    'WALLET',
    'WALLET',
    {refund.refund_ref},
    false
FROM user_wallets uw
WHERE uw.user_id = {booking.UserId}
  AND NOT EXISTS
  (
      SELECT 1
      FROM wallet_transactions wt
      WHERE wt.refund_id = {refund.id}
        AND wt.transaction_type = 'REFUND'
        AND wt.entry_type = 'CREDIT'
        AND wt.is_deleted = false
  );");
}

private string BuildAbsoluteUrl(string pathAndQuery)
{
    return $"{GetPublicBaseUrl()}{pathAndQuery}";
}

private string GetPublicBaseUrl()
{
    var host =
    Request.Host;

    var hostName =
    host.Host;

    var isLoopback =
    string.Equals(hostName,"localhost",StringComparison.OrdinalIgnoreCase)
    || string.Equals(hostName,"127.0.0.1",StringComparison.OrdinalIgnoreCase)
    || string.Equals(hostName,"::1",StringComparison.OrdinalIgnoreCase);

    if(!isLoopback)
    {
        return $"{Request.Scheme}://{host}";
    }

    var lanIp =
    GetLocalLanIpAddress();

    if(string.IsNullOrWhiteSpace(lanIp))
    {
        return $"{Request.Scheme}://{host}";
    }

    return $"{Request.Scheme}://{lanIp}:{host.Port ?? 5089}";
}

private static string? GetLocalLanIpAddress()
{
    foreach(var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
    {
        if(networkInterface.OperationalStatus!=OperationalStatus.Up)
        {
            continue;
        }

        var properties =
        networkInterface.GetIPProperties();

        foreach(var address in properties.UnicastAddresses)
        {
            if(address.Address.AddressFamily!=AddressFamily.InterNetwork)
            {
                continue;
            }

            if(IPAddress.IsLoopback(address.Address))
            {
                continue;
            }

            return address.Address.ToString();
        }
    }

    return null;
}
    }
}
