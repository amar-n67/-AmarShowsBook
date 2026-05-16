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

            var seats=

            await(

            from s in _context.ScreenSeats

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

            var booking=
            new BookingDraft
            {
                UserId=userId,
                ScheduleId=request.ScheduleId,
                SeatNumbers=
                string.Join(",",request.SeatIds),

                TotalAmount=
                request.TotalAmount,

                Status="PENDING",

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

public async Task<IActionResult>
Details(long id)
{
    var booking =

    await _context
    .BookingDrafts
    .FirstOrDefaultAsync(
    x=>x.Id==id);

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

            return View(
            booking);
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

    session.Status=
    "APPROVED";

    _context.BookingTransactions.Add(

    new BookingTransaction
    {
        BookingId=
        booking.Id,

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
    "REJECTED";

    await _context.SaveChangesAsync();

    return Json(new
    {
        success=true
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

            var transaction=
            new BookingTransaction
            {
                BookingId=
                booking.Id,

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

            return View(booking);
        }
    }
}