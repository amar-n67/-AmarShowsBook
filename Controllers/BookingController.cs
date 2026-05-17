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

var expiredLocks=

await _context.SeatLocks
.Where(
x=>
x.LockStatus=="LOCKED"
&&
x.ExpiresAt<DateTime.UtcNow
)
.ToListAsync();

if(expiredLocks.Any())
{
    _context.SeatLocks
    .RemoveRange(expiredLocks);

    await _context.SaveChangesAsync();
}
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

_context.BookingDrafts.Any(
b=>
b.ScheduleId==id
&&
b.Status=="CONFIRMED"
&&
(","+b.SeatNumbers+",")
.Contains(
","+
s.SeatRow+
s.SeatNumber+
","
)
),

IsLocked=

lockSeat!=null
&&
lockSeat.LockStatus=="LOCKED"
&&
lockSeat.ExpiresAt>DateTime.UtcNow
    }

    ).ToListAsync();

    ViewBag.Seats=seats;

    return View(schedule);
}

        // ==========================================
        // LOCK SEATS
        // ==========================================

        [HttpPost]
        public async Task<IActionResult>LockSeats(
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
x.LockStatus=="LOCKED"
&&
x.ExpiresAt>DateTime.UtcNow
);

var seat=

await _context.ScreenSeats
.FirstOrDefaultAsync(
x=>x.Id==seatId
);

string seatName=
seat.SeatRow+
seat.SeatNumber;


bool alreadyBooked=

await _context.BookingDrafts
.AnyAsync(
x=>
x.ScheduleId==
request.ScheduleId
&&
x.Status=="CONFIRMED"
&&
x.SeatNumbers.Contains(
seatName
)
);


if(exists||alreadyBooked)
{
    return Json(
    new
    {
        success=false,
        message=
        "Seat unavailable"
    });
}

                _context.SeatLocks.Add(
                new SeatLock
                {
                    ScheduleId=
                    request.ScheduleId,

                    ScreenSeatId=
                    seatId,

                    UserId=
                    userId,

                    LockStatus=
                    "LOCKED",

                    ExpiresAt=
                    DateTime.UtcNow
                    .AddMinutes(5)
                });
            }

            await _context.SaveChangesAsync();

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

public async Task<IActionResult>Details(long id)
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
    var bookings=

    _context
    .VwBookingCompleteDetails
    .AsNoTracking()
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
        [HttpPost]
public async Task<IActionResult>
ReleaseExpiredLocks()
{
    var expired=

    await _context.SeatLocks
    .Where(
    x=>
    x.ExpiresAt<DateTime.UtcNow
    )
    .ToListAsync();

    _context.SeatLocks
    .RemoveRange(expired);

    await _context.SaveChangesAsync();

    return Ok();
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
    var locks=

await _context.SeatLocks
.Where(
x=>
x.ScheduleId==
booking.ScheduleId
&&
x.UserId==
booking.UserId
)
.ToListAsync();

_context.SeatLocks.RemoveRange(
locks
);

    session.Status="SUCCESS";

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