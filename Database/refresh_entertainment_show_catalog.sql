BEGIN;

WITH protected_schedules AS (
    SELECT schedule_id AS id FROM public.bookings WHERE schedule_id IS NOT NULL
    UNION
    SELECT "ScheduleId" AS id FROM public.booking_drafts WHERE "ScheduleId" IS NOT NULL
),
removable_schedules AS (
    SELECT "Id" AS id
    FROM public."ShowSchedules"
    WHERE "Id" NOT IN (SELECT id FROM protected_schedules)
)
DELETE FROM public.screen_seats
WHERE "ScheduleId" IN (SELECT id FROM removable_schedules);

WITH protected_schedules AS (
    SELECT schedule_id AS id FROM public.bookings WHERE schedule_id IS NOT NULL
    UNION
    SELECT "ScheduleId" AS id FROM public.booking_drafts WHERE "ScheduleId" IS NOT NULL
),
removable_schedules AS (
    SELECT "Id" AS id
    FROM public."ShowSchedules"
    WHERE "Id" NOT IN (SELECT id FROM protected_schedules)
)
DELETE FROM public.show_seat_pricing
WHERE schedule_id IN (SELECT id FROM removable_schedules);

WITH protected_schedules AS (
    SELECT schedule_id AS id FROM public.bookings WHERE schedule_id IS NOT NULL
    UNION
    SELECT "ScheduleId" AS id FROM public.booking_drafts WHERE "ScheduleId" IS NOT NULL
),
removable_schedules AS (
    SELECT "Id" AS id
    FROM public."ShowSchedules"
    WHERE "Id" NOT IN (SELECT id FROM protected_schedules)
)
DELETE FROM public.booked_seats
WHERE schedule_id IN (SELECT id FROM removable_schedules);

WITH protected_schedules AS (
    SELECT schedule_id AS id FROM public.bookings WHERE schedule_id IS NOT NULL
    UNION
    SELECT "ScheduleId" AS id FROM public.booking_drafts WHERE "ScheduleId" IS NOT NULL
),
removable_schedules AS (
    SELECT "Id" AS id
    FROM public."ShowSchedules"
    WHERE "Id" NOT IN (SELECT id FROM protected_schedules)
)
DELETE FROM public.seat_locks
WHERE schedule_id IN (SELECT id FROM removable_schedules);

WITH protected_schedules AS (
    SELECT schedule_id AS id FROM public.bookings WHERE schedule_id IS NOT NULL
    UNION
    SELECT "ScheduleId" AS id FROM public.booking_drafts WHERE "ScheduleId" IS NOT NULL
)
DELETE FROM public."ShowSchedules"
WHERE "Id" NOT IN (SELECT id FROM protected_schedules);

DELETE FROM public."Movies"
WHERE "Id" NOT IN (
    SELECT "MovieId" FROM public."ShowSchedules" WHERE "MovieId" IS NOT NULL
);

DELETE FROM public."StandupShows"
WHERE "Id" NOT IN (
    SELECT "StandupShowId" FROM public."ShowSchedules" WHERE "StandupShowId" IS NOT NULL
);

DELETE FROM public."LiveStreams"
WHERE "Id" NOT IN (
    SELECT "LiveStreamId" FROM public."ShowSchedules" WHERE "LiveStreamId" IS NOT NULL
);

WITH movie_seed(title, director, producer, cast_names, duration, imdb_rating, video_id, description) AS (
    VALUES
    ('Oppenheimer', 'Christopher Nolan', 'Universal Pictures', 'Cillian Murphy, Emily Blunt, Robert Downey Jr.', 181, 8.3, 'uYPbbksJxIg', 'A biographical thriller about J. Robert Oppenheimer and the creation of the atomic bomb.'),
    ('Dune: Part Two', 'Denis Villeneuve', 'Warner Bros. Pictures', 'Timothee Chalamet, Zendaya, Rebecca Ferguson', 166, 8.5, 'Way9Dexny3w', 'Paul Atreides unites with Chani and the Fremen while seeking revenge and confronting a terrible future.'),
    ('Inside Out 2', 'Kelsey Mann', 'Disney/Pixar', 'Amy Poehler, Maya Hawke, Kensington Tallman', 96, 7.5, 'LEjhY15eCx0', 'Riley becomes a teenager and headquarters must make room for new emotions.'),
    ('Spider-Man: Across the Spider-Verse', 'Joaquim Dos Santos, Kemp Powers, Justin K. Thompson', 'Sony Pictures Animation', 'Shameik Moore, Hailee Steinfeld, Oscar Isaac', 140, 8.6, 'cqGjhVJWtEg', 'Miles Morales travels across the multiverse and meets a team of Spider-People.'),
    ('Interstellar', 'Christopher Nolan', 'Paramount Pictures', 'Matthew McConaughey, Anne Hathaway, Jessica Chastain', 169, 8.7, 'zSWdZVtXT7E', 'Explorers travel through a wormhole in search of a new home for humanity.'),
    ('Inception', 'Christopher Nolan', 'Warner Bros. Pictures', 'Leonardo DiCaprio, Joseph Gordon-Levitt, Elliot Page', 148, 8.8, 'YoHD9XEInc0', 'A skilled thief enters dreams to plant an idea in a target mind.'),
    ('RRR', 'S. S. Rajamouli', 'DVV Entertainment', 'N. T. Rama Rao Jr., Ram Charan, Alia Bhatt', 182, 7.8, 'f_vbAtFSEc0', 'Two revolutionaries form a fierce friendship before joining the fight against colonial power.'),
    ('Top Gun: Maverick', 'Joseph Kosinski', 'Paramount Pictures', 'Tom Cruise, Miles Teller, Jennifer Connelly', 130, 8.2, 'qSqVVswa420', 'Pete Maverick Mitchell trains a new generation of pilots for a dangerous mission.'),
    ('Everything Everywhere All at Once', 'Daniel Kwan, Daniel Scheinert', 'A24', 'Michelle Yeoh, Ke Huy Quan, Stephanie Hsu', 139, 7.8, 'wxN1T1uxQ2g', 'A laundromat owner is pulled into a wild multiverse adventure.'),
    ('Avatar: The Way of Water', 'James Cameron', '20th Century Studios', 'Sam Worthington, Zoe Saldana, Sigourney Weaver', 192, 7.5, 'd9MyW72ELq0', 'The Sully family explores Pandora''s oceans while facing a renewed threat.')
)
INSERT INTO public."Movies" ("Title", "Director", "Producer", "Cast", "Duration", "Description", "PosterUrl", "Images", "TrailerUrl", "ImdbRating")
SELECT
    title,
    director,
    producer,
    cast_names,
    duration,
    description,
    'https://img.youtube.com/vi/' || video_id || '/hqdefault.jpg',
    'https://img.youtube.com/vi/' || video_id || '/mqdefault.jpg,https://img.youtube.com/vi/' || video_id || '/0.jpg',
    'https://www.youtube.com/watch?v=' || video_id,
    imdb_rating
FROM movie_seed;

WITH standup_seed(title, comedian, duration, video_id, description) AS (
    VALUES
    ('Vir Das: Landing', 'Vir Das', 66, 'EiHqOV-bHSk', 'Vir Das reflects on India, identity, outrage and finding his feet in the world.'),
    ('Vir Das: Fool Volume', 'Vir Das', 70, 'XA_CdOkiuKU', 'A global comedy special about silence, joy, kindness and rediscovering a voice.'),
    ('Trevor Noah: Where Was I', 'Trevor Noah', 68, 'U6Bn9yRwzKw', 'Trevor Noah turns world travel, language and culture into sharp stand-up storytelling.'),
    ('Hannah Gadsby: Nanette', 'Hannah Gadsby', 69, '5aE29fiatQ0', 'A genre-shifting stand-up special blending comedy, art history and personal truth.'),
    ('Taylor Tomlinson: Look At You', 'Taylor Tomlinson', 60, '6UaUdWmTNGY', 'Taylor Tomlinson jokes about mental health, dating and growing into adulthood.'),
    ('Hasan Minhaj: The King''s Jester', 'Hasan Minhaj', 60, 'KDHV7nHMmIk', 'Hasan Minhaj unpacks family, fame and the cost of speaking your mind.'),
    ('Ronny Chieng: Speakeasy', 'Ronny Chieng', 60, 'JK6p94ddkJ8', 'Ronny Chieng brings fast, precise stand-up about modern life and cultural absurdity.'),
    ('Kenny Sebastian: The Most Interesting Person in the Room', 'Kenny Sebastian', 67, '1Upgl-MYCFM', 'Kenny Sebastian mixes music, observations and self-aware stories from everyday life.')
)
INSERT INTO public."StandupShows" ("Title", "Comedian", "Duration", "Description", "PosterUrl", "Images", "TrailerUrl")
SELECT
    title,
    comedian,
    duration,
    description,
    'https://img.youtube.com/vi/' || video_id || '/hqdefault.jpg',
    'https://img.youtube.com/vi/' || video_id || '/mqdefault.jpg,https://img.youtube.com/vi/' || video_id || '/0.jpg',
    'https://www.youtube.com/watch?v=' || video_id
FROM standup_seed;

WITH live_seed(title, host_name, duration, video_id, description) AS (
    VALUES
    ('Taylor Swift | The Eras Tour', 'Taylor Swift Productions', 169, 'KudedLV0tP0', 'A concert-film experience built from Taylor Swift''s record-breaking Eras Tour.'),
    ('Taylor Swift | The Eras Tour | The Final Show', 'Disney+', 180, '8XrF1uvcFfE', 'The final Vancouver concert presentation featuring the expanded Eras Tour set.'),
    ('Coldplay: Music of the Spheres Live at River Plate', 'Coldplay', 138, 'tO7CCP7liwI', 'Coldplay''s Music of the Spheres World Tour captured at River Plate Stadium.'),
    ('BTS: Yet To Come in Cinemas', 'HYBE', 103, '9uOMectkCCs', 'A cinematic presentation of BTS performing Yet To Come in Busan.'),
    ('Metallica: M72 World Tour Live from Arlington', 'Metallica', 150, 'YJoIbP38vMQ', 'A big-screen live concert presentation from Metallica''s M72 World Tour.'),
    ('Billie Eilish: Live at The O2', 'Billie Eilish', 99, 'LPY4jB2F9Go', 'Billie Eilish performs her Happier Than Ever tour on the London O2 stage.'),
    ('Hans Zimmer Live', 'Hans Zimmer', 150, 'va1oiojnGrA', 'Hans Zimmer and his band perform cinematic music from his most famous scores.'),
    ('Shakira: Live & Off the Record', 'Shakira', 90, 'DUT5rEU6pqM', 'A concert event centered on Shakira''s stage performance and global hits.')
)
INSERT INTO public."LiveStreams" ("Title", "Host", "Duration", "Description", "PosterUrl", "Images", "TrailerUrl")
SELECT
    title,
    host_name,
    duration,
    description,
    'https://img.youtube.com/vi/' || video_id || '/hqdefault.jpg',
    'https://img.youtube.com/vi/' || video_id || '/mqdefault.jpg,https://img.youtube.com/vi/' || video_id || '/0.jpg',
    'https://www.youtube.com/watch?v=' || video_id
FROM live_seed;

WITH calendar_days AS (
    SELECT generate_series(date '2026-08-28', date '2026-12-31', interval '1 day')::date AS show_date
),
slots(slot_no, show_type, local_time) AS (
    VALUES
    (1, 'Movie', time '10:00'),
    (2, 'Standup', time '13:15'),
    (3, 'Live', time '16:30'),
    (4, 'Movie', time '19:30'),
    (5, 'Standup', time '22:15')
),
numbered AS (
    SELECT
        d.show_date,
        s.slot_no,
        s.show_type,
        s.local_time,
        row_number() OVER (ORDER BY d.show_date) AS day_index
    FROM calendar_days d
    CROSS JOIN slots s
),
screen_pool AS (
    SELECT id, row_number() OVER (ORDER BY id) AS rn, count(*) OVER () AS total
    FROM public.screens
    WHERE is_active = true
),
location_pool AS (
    SELECT "Id", row_number() OVER (ORDER BY "Id") AS rn, count(*) OVER () AS total
    FROM public."Locations"
),
movie_pick AS (
    SELECT "Id", "Duration", row_number() OVER (ORDER BY "Id") AS rn, count(*) OVER () AS total
    FROM public."Movies"
    WHERE "Title" IN (
        'Oppenheimer',
        'Dune: Part Two',
        'Inside Out 2',
        'Spider-Man: Across the Spider-Verse',
        'Interstellar',
        'Inception',
        'RRR',
        'Top Gun: Maverick',
        'Everything Everywhere All at Once',
        'Avatar: The Way of Water'
    )
    AND "PosterUrl" LIKE 'https://img.youtube.com/vi/%'
),
standup_pick AS (
    SELECT "Id", "Duration", row_number() OVER (ORDER BY "Id") AS rn, count(*) OVER () AS total
    FROM public."StandupShows"
    WHERE "Title" IN (
        'Vir Das: Landing',
        'Vir Das: Fool Volume',
        'Trevor Noah: Where Was I',
        'Hannah Gadsby: Nanette',
        'Taylor Tomlinson: Look At You',
        'Hasan Minhaj: The King''s Jester',
        'Ronny Chieng: Speakeasy',
        'Kenny Sebastian: The Most Interesting Person in the Room'
    )
    AND "PosterUrl" LIKE 'https://img.youtube.com/vi/%'
),
live_pick AS (
    SELECT "Id", "Duration", row_number() OVER (ORDER BY "Id") AS rn, count(*) OVER () AS total
    FROM public."LiveStreams"
    WHERE "Title" IN (
        'Taylor Swift | The Eras Tour',
        'Taylor Swift | The Eras Tour | The Final Show',
        'Coldplay: Music of the Spheres Live at River Plate',
        'BTS: Yet To Come in Cinemas',
        'Metallica: M72 World Tour Live from Arlington',
        'Billie Eilish: Live at The O2',
        'Hans Zimmer Live',
        'Shakira: Live & Off the Record'
    )
    AND "PosterUrl" LIKE 'https://img.youtube.com/vi/%'
)
INSERT INTO public."ShowSchedules" ("MovieId", "StandupShowId", "LiveStreamId", "LocationId", "StartTime", "EndTime", "Type", screen_id, "ShowDay")
SELECT
    CASE WHEN n.show_type = 'Movie' THEN mp."Id" END,
    CASE WHEN n.show_type = 'Standup' THEN sp."Id" END,
    CASE WHEN n.show_type = 'Live' THEN lp."Id" END,
    loc."Id",
    ((n.show_date + n.local_time) AT TIME ZONE 'Asia/Kolkata') AS start_time,
    ((n.show_date + n.local_time) AT TIME ZONE 'Asia/Kolkata')
        + make_interval(mins => CASE
            WHEN n.show_type = 'Movie' THEN mp."Duration"
            WHEN n.show_type = 'Standup' THEN sp."Duration"
            ELSE lp."Duration"
        END) AS end_time,
    n.show_type,
    scr.id,
    to_char(n.show_date, 'FMDay')
FROM numbered n
JOIN location_pool loc ON loc.rn = (((n.day_index + n.slot_no - 2) % loc.total) + 1)
LEFT JOIN screen_pool scr ON scr.rn = (((n.day_index * 5 + n.slot_no - 6) % NULLIF(scr.total, 0)) + 1)
LEFT JOIN movie_pick mp ON n.show_type = 'Movie' AND mp.rn = (((n.day_index + n.slot_no - 2) % mp.total) + 1)
LEFT JOIN standup_pick sp ON n.show_type = 'Standup' AND sp.rn = (((n.day_index + n.slot_no - 2) % sp.total) + 1)
LEFT JOIN live_pick lp ON n.show_type = 'Live' AND lp.rn = (((n.day_index + n.slot_no - 2) % lp.total) + 1)
ORDER BY n.show_date, n.slot_no;

COMMIT;
