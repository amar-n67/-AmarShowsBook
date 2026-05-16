using AmarShowsBook.Data;
using AmarShowsBook.Models;
using AmarShowsBook.Models.ViewModels;
using AmarShowsBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        // =====================================================
        // SEAT PAGE
        // =====================================================

        [Route("Booking/Seats/{id}")]
        public async Task<IActionResult> Seats(int id)
        {
            var schedule =
            await _context.ShowSchedules
            .Include(x=>x.Movie)
            .Include(x=>x.StandupShow)
            .Include(x=>x.LiveStream)
            .Include(x=>x.Location)
            .FirstOrDefaultAsync(
                x=>x.Id==id
            );

            if(schedule==null)
            {
                return NotFound();
            }

            var seats=

            await(

            from s in _context.ScreenSeats

            join l in _context.SeatLocks
            on s.Id equals l.ScreenSeatId
            into seatLockGroup

            from lockSeat in
            seatLockGroup.DefaultIfEmpty()

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
                lockSeat.LockStatus==
                "CONFIRMED",

                IsLocked=
                lockSeat!=null
                &&
                lockSeat.LockStatus==
                "LOCKED"
            })

            .ToListAsync();

            ViewBag.Seats=seats;

            return View(schedule);
        }

        // =====================================================
        // LOCK SEATS
        // =====================================================

        [HttpPost]
        public async Task<IActionResult>
        LockSeats(
        [FromBody]
        SeatLockRequest request)
        {
            var userIdText=
            HttpContext.Session
            .GetString("UserId");

            var userId=
            long.TryParse(
            userIdText,
            out long parsedId)
            ?
            parsedId
            :
            0;


            foreach(
            var seatId
            in request.SeatIds)
            {
                var exists=

                await _context
                .SeatLocks
                .AnyAsync(
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
                )
                );


                if(exists)
                {
                    return Json(
                    new
                    {
                        success=false,
                        message=
                        "Seat already booked"
                    });
                }


                _context.SeatLocks.Add(

                new SeatLock
                {
                    UserId=userId,

                    ScheduleId=
                    request.ScheduleId,

                    ScreenSeatId=
                    seatId,

                    LockedAt=
                    DateTime.UtcNow,

                    ExpiresAt=
                    DateTime.UtcNow
                    .AddMinutes(5),

                    LockStatus=
                    "LOCKED"
                });
            }

            await _context
            .SaveChangesAsync();


            var booking=
            new BookingDraft
            {
                UserId=userId,

                ScheduleId=
                request.ScheduleId,

                SeatNumbers=
                string.Join(
                ",",
                request.SeatIds),

                TotalAmount=
                request.TotalAmount,

                Status=
                "PENDING",

                CreatedAt=
                DateTime.UtcNow
            };


            _context.BookingDrafts
            .Add(booking);

            await _context
            .SaveChangesAsync();


            return Json(
            new
            {
                success=true,
                bookingId=
                booking.Id
            });
        }

        // =====================================================
        // DETAILS PAGE
        // =====================================================

        public async Task<IActionResult>
        Details(long id)
        {
            var booking=

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
            .Include(x=>x.Location)
            .FirstAsync(
            x=>
            x.Id==
            booking.ScheduleId
            );

            ViewBag.Schedule=
            schedule;

            return View(
            booking);
        }


        // =====================================================
        // PAYMENT PAGE
        // =====================================================

        public async Task<IActionResult>
        Payment(
        long bookingId)
        {
            var booking=

            await _context
            .BookingDrafts
            .FindAsync(
            bookingId);

            if(booking==null)
            {
                return NotFound();
            }

            return View(
            booking);
        }


        // =====================================================
        // COMPLETE PAYMENT
        // =====================================================

       [HttpPost]
public async Task<IActionResult> CompletePayment(
    [FromBody] PaymentRequest request)
{
    var booking = await _context
        .BookingDrafts
        .FindAsync(request.BookingId);

    if (booking == null)
    {
        return BadRequest(new
        {
            success = false,
            message = "Booking not found"
        });
    }

    var transaction = new BookingTransaction
    {
        BookingId = booking.Id,
        TransactionRef = Guid.NewGuid().ToString(),
        PaymentMethod = request.PaymentMethod,
        Amount = booking.TotalAmount,
        PaymentStatus = "SUCCESS",
        CreatedAt = DateTime.UtcNow,
        PaidAt = DateTime.UtcNow
    };

    _context.BookingTransactions.Add(transaction);

    booking.Status = "CONFIRMED";

    await _context.SaveChangesAsync();

    return Ok(new
    {
        success = true
    });
}

        // =====================================================
        // MY BOOKINGS
        // =====================================================

        public IActionResult MyBookings()
        {
            var userEmail=
            HttpContext.Session
            .GetString("UserEmail");

            if(
            string.IsNullOrWhiteSpace(
            userEmail))
            {
                return RedirectToAction(
                "Login",
                "Auth");
            }

            var bookings=

            _context
            .VwBookingCompleteDetails
            .AsNoTracking()
            .Where(
            x=>

            x.UserEmail
            .ToLower()==
            userEmail
            .ToLower()
            )
            .OrderByDescending(
            x=>x.BookedAt)
            .ToList();

            return View(bookings);
        }
    }
}