using AmarShowsBook.Data;
using AmarShowsBook.Models;
using AmarShowsBook.Models.ViewModels;
using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace AmarShowsBook.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger;

        public BookingController(
            ApplicationDbContext context,
            IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }


        // ==========================================
        // SEATS
        // ==========================================

        [Route("Booking/Seats/{id}")]
public async Task<IActionResult> Seats(int id)
{
    var schedule =
    await _context.ShowSchedules
    .Include(x=>x.Movie)
    .Include(x=>x.StandupShow)
    .Include(x=>x.LiveStream)
    .Include(x=>x.Location)
    .FirstOrDefaultAsync(x=>x.Id==id);

    if(schedule==null)
        return NotFound();


    // ==================================
    // Create seats for this schedule only
    // ==================================

    bool seatsExist=

    await _context.ScreenSeats
    .AnyAsync(
    x=>x.ScheduleId==id);

    if(!seatsExist)
    {
        await GenerateSeats(
        id,
        schedule.Type);
    }


    // ==================================
    // Load only this schedule seats
    // ==================================

    var seats=

    await(

    from s in _context.ScreenSeats

    where s.ScheduleId==id

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

    ViewBag.Seats=seats;

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
    .FirstOrDefaultAsync(
    x=>x.Id==booking.ScheduleId);


    var user=

    await _context
    .Users
    .FirstOrDefaultAsync(
    x=>x.Id==booking.UserId);


    ViewBag.Schedule=
    schedule;

    ViewBag.User=
    user;

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

            return View(booking);
        }
// ==========================================
// MY BOOKINGS
// ==========================================

public IActionResult MyBookings()
{
    var userIdText =
    HttpContext.Session.GetString("UserId");

    if(!long.TryParse(userIdText,out var userId))
    {
        return RedirectToAction("Login","Auth");
    }

    var bookings=

    _context
    .VwBookingCompleteDetails
    .AsNoTracking()
    .Where(x=>x.UserId==userId)
    .OrderByDescending(
    x=>x.BookedAt)
    .ToList();

    return View(
    bookings);
}


        // ==========================================
        // GENERATE QR
        // ==========================================

        public async Task<IActionResult>
        GenerateQR(long bookingId)
        {
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

            // var url=

            // $"{Request.Scheme}://{Request.Host}/Booking/MobilePay?token={token}";
            string ip = "192.168.1.2"; // replace with your Mac IP

            var url =
            $"http://{ip}:5089/Booking/MobilePay?token={token}";
            return Json(new
            {
                url
            });
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


    var booking=

    await _context
    .BookingDrafts
    .FindAsync(
    session.BookingId);


    var schedule=

    await _context
    .ShowSchedules
    .Include(x=>x.Movie)
    .Include(x=>x.StandupShow)
    .Include(x=>x.LiveStream)
    .Include(x=>x.Location)
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

    ViewBag.User=
    user;

    return View(
    booking);
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
string paymentMethod)
{
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
            DateTime.SpecifyKind(
            DateTime.UtcNow,
            DateTimeKind.Unspecified);

            existingTransaction =
            new Transaction
            {
                TransactionRef=$"TXN-{draft.Id}-{nowForMissingTransaction:yyyyMMddHHmmssfff}",
                UserId=draft.UserId,
                TransactionType="BOOKING",
                PaymentMethod=paymentMethod,
                Amount=draft.TotalAmount,
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
        var repairedAt =
        DateTime.SpecifyKind(
        DateTime.UtcNow,
        DateTimeKind.Unspecified);

        existingBooking.UpdatedAt=repairedAt;
        existingBooking.ConfirmedAt ??= repairedAt;

        await _context.SaveChangesAsync();

        return existingBooking;
    }

    await using var dbTransaction =
    await _context.Database.BeginTransactionAsync();

    var now =
    DateTime.SpecifyKind(
    DateTime.UtcNow,
    DateTimeKind.Unspecified);

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
        DiscountAmount=0,
        PayableAmount=draft.TotalAmount,
        TaxAmount=0,
        ConvenienceFee=0,
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
        Amount=draft.TotalAmount,
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
            success=false
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
            success=false
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
        "QR");

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
            success=false
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
                request.PaymentMethod);

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

            return Json(new
            {
                success=true
            });
        }
// ==========================================
// CONFIRMATION
// ==========================================

public async Task<IActionResult>
Confirmation(long bookingId)
{
    var booking=

    await _context
    .BookingDrafts
    .FirstOrDefaultAsync(
    x=>x.Id==bookingId);

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
    .FirstOrDefaultAsync(
    x=>x.Id==
    booking.ScheduleId);

    ViewBag.Schedule=
    schedule;

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
    }
}
