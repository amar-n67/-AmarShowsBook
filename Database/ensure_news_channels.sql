CREATE TABLE IF NOT EXISTS public.news_channels
(
    id bigserial PRIMARY KEY,
    channel_code varchar(80) NOT NULL UNIQUE,
    channel_name varchar(180) NOT NULL,
    language varchar(80) NOT NULL,
    category varchar(80) NOT NULL,
    region varchar(120) NOT NULL,
    country varchar(120) NOT NULL DEFAULT 'India',
    state varchar(120) NOT NULL DEFAULT 'All',
    city varchar(120) NOT NULL DEFAULT 'All',
    description text,
    logo_url text,
    website_url text,
    live_url text,
    sort_order integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS public.news_broadcast_slots
(
    id bigserial PRIMARY KEY,
    channel_id bigint NOT NULL REFERENCES public.news_channels(id) ON DELETE CASCADE,
    program_title varchar(180) NOT NULL,
    program_type varchar(80) NOT NULL,
    starts_at timestamp with time zone NOT NULL,
    ends_at timestamp with time zone NOT NULL,
    is_live boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE public.news_channels ADD COLUMN IF NOT EXISTS country varchar(120) NOT NULL DEFAULT 'India';
ALTER TABLE public.news_channels ADD COLUMN IF NOT EXISTS state varchar(120) NOT NULL DEFAULT 'All';
ALTER TABLE public.news_channels ADD COLUMN IF NOT EXISTS city varchar(120) NOT NULL DEFAULT 'All';

WITH seed(channel_code, channel_name, language, category, region, country, state_name, city, website_url, live_url, sort_order) AS (
    VALUES
    ('AAJ_TAK', 'Aaj Tak', 'Hindi', 'National', 'India', 'India', 'All', 'All', 'https://www.aajtak.in', 'https://www.youtube.com/@aajtak', 10),
    ('NDTV_24X7', 'NDTV 24x7', 'English', 'National', 'India', 'India', 'All', 'All', 'https://www.ndtv.com', 'https://www.youtube.com/@NDTV', 20),
    ('NDTV_INDIA', 'NDTV India', 'Hindi', 'National', 'India', 'India', 'All', 'All', 'https://ndtv.in', 'https://www.youtube.com/@ndtvindia', 30),
    ('INDIA_TODAY', 'India Today', 'English', 'National', 'India', 'India', 'All', 'All', 'https://www.indiatoday.in', 'https://www.youtube.com/@indiatoday', 40),
    ('REPUBLIC_TV', 'Republic TV', 'English', 'National', 'India', 'India', 'All', 'All', 'https://www.republicworld.com', 'https://www.youtube.com/@RepublicWorld', 50),
    ('REPUBLIC_BHARAT', 'Republic Bharat', 'Hindi', 'National', 'India', 'India', 'All', 'All', 'https://www.republicbharat.com', 'https://www.youtube.com/@RepublicBharat', 60),
    ('TIMES_NOW', 'Times Now', 'English', 'National', 'India', 'India', 'All', 'All', 'https://www.timesnownews.com', 'https://www.youtube.com/@TimesNow', 70),
    ('CNN_NEWS18', 'CNN-News18', 'English', 'National', 'India', 'India', 'All', 'All', 'https://www.news18.com', 'https://www.youtube.com/@cnnnews18', 80),
    ('NEWS18_INDIA', 'News18 India', 'Hindi', 'National', 'India', 'India', 'All', 'All', 'https://hindi.news18.com', 'https://www.youtube.com/@news18India', 90),
    ('INDIA_TV', 'India TV', 'Hindi', 'National', 'India', 'India', 'All', 'All', 'https://www.indiatvnews.com', 'https://www.youtube.com/@IndiaTV', 100),
    ('ZEE_NEWS', 'Zee News', 'Hindi', 'National', 'India', 'India', 'All', 'All', 'https://zeenews.india.com', 'https://www.youtube.com/@ZeeNews', 110),
    ('ABP_NEWS', 'ABP News', 'Hindi', 'National', 'India', 'India', 'All', 'All', 'https://www.abplive.com', 'https://www.youtube.com/@ABPNEWS', 120),
    ('DD_NEWS', 'DD News', 'Hindi/English', 'Public Broadcaster', 'India', 'India', 'All', 'All', 'https://ddnews.gov.in', 'https://www.youtube.com/@DDNewsOfficial', 130),
    ('SANSAD_TV', 'Sansad TV', 'Hindi/English', 'Public Broadcaster', 'India', 'India', 'All', 'All', 'https://sansadtv.nic.in', 'https://www.youtube.com/@SansadTV', 140),
    ('WION', 'WION', 'English', 'International', 'India/World', 'India', 'All', 'All', 'https://www.wionews.com', 'https://www.youtube.com/@WION', 150),
    ('CNBC_TV18', 'CNBC-TV18', 'English', 'Business', 'India', 'India', 'All', 'All', 'https://www.cnbctv18.com', 'https://www.youtube.com/@CNBCTV18', 160),
    ('ET_NOW', 'ET Now', 'English', 'Business', 'India', 'India', 'All', 'All', 'https://www.etnownews.com', 'https://www.youtube.com/@ETNOW', 170),
    ('MIRROR_NOW', 'Mirror Now', 'English', 'National', 'India', 'India', 'All', 'All', 'https://www.timesnownews.com/mirror-now', 'https://www.youtube.com/@MirrorNow', 180),
    ('TV9_BHARATVARSH', 'TV9 Bharatvarsh', 'Hindi', 'National', 'India', 'India', 'All', 'All', 'https://www.tv9hindi.com', 'https://www.youtube.com/@TV9Bharatvarsh', 190),
    ('GOOD_NEWS_TODAY', 'Good News Today', 'Hindi', 'National', 'India', 'India', 'All', 'All', 'https://www.gnttv.com', 'https://www.youtube.com/@GoodNewsToday', 200),
    ('BBC_NEWS', 'BBC News', 'English', 'International', 'World', 'World', 'All', 'All', 'https://www.bbc.com/news', 'https://www.youtube.com/@BBCNews', 210),
    ('AL_JAZEERA', 'Al Jazeera English', 'English', 'International', 'World', 'World', 'All', 'All', 'https://www.aljazeera.com', 'https://www.youtube.com/@aljazeeraenglish', 220),
    ('SKY_NEWS', 'Sky News', 'English', 'International', 'World', 'World', 'All', 'All', 'https://news.sky.com', 'https://www.youtube.com/@SkyNews', 230),
    ('FRANCE_24', 'France 24 English', 'English', 'International', 'World', 'World', 'All', 'All', 'https://www.france24.com/en', 'https://www.youtube.com/@France24_en', 240),
    ('TV9_TELUGU', 'TV9 Telugu', 'Telugu', 'Regional', 'Telangana/Andhra Pradesh', 'India', 'Telangana', 'Hyderabad', 'https://www.tv9telugu.com', 'https://www.youtube.com/@TV9TeluguLive', 250),
    ('NTV_TELUGU', 'NTV Telugu', 'Telugu', 'Regional', 'Telangana/Andhra Pradesh', 'India', 'Telangana', 'Hyderabad', 'https://ntvtelugu.com', 'https://www.youtube.com/@ntvtelugulive', 260),
    ('SAKSHI_TV', 'Sakshi TV', 'Telugu', 'Regional', 'Andhra Pradesh/Telangana', 'India', 'Andhra Pradesh', 'Vijayawada', 'https://www.sakshi.com', 'https://www.youtube.com/@sakshitv', 270),
    ('SUN_NEWS', 'Sun News', 'Tamil', 'Regional', 'Tamil Nadu', 'India', 'Tamil Nadu', 'Chennai', 'https://www.sunnewslive.in', 'https://www.youtube.com/@sunnewstamil', 280),
    ('POLIMER_NEWS', 'Polimer News', 'Tamil', 'Regional', 'Tamil Nadu', 'India', 'Tamil Nadu', 'Chennai', 'https://www.polimernews.com', 'https://www.youtube.com/@polimernews', 290),
    ('ASIANET_NEWS', 'Asianet News', 'Malayalam', 'Regional', 'Kerala', 'India', 'Kerala', 'Kochi', 'https://www.asianetnews.com', 'https://www.youtube.com/@AsianetNews', 300),
    ('TV9_KANNADA', 'TV9 Kannada', 'Kannada', 'Regional', 'Karnataka', 'India', 'Karnataka', 'Bengaluru', 'https://tv9kannada.com', 'https://www.youtube.com/@tv9kannada', 310),
    ('PUBLIC_TV', 'Public TV', 'Kannada', 'Regional', 'Karnataka', 'India', 'Karnataka', 'Bengaluru', 'https://publictv.in', 'https://www.youtube.com/@publictv', 320),
    ('ABP_MAJHA', 'ABP Majha', 'Marathi', 'Regional', 'Maharashtra', 'India', 'Maharashtra', 'Mumbai', 'https://marathi.abplive.com', 'https://www.youtube.com/@abpmajhatv', 330),
    ('TV9_MARATHI', 'TV9 Marathi', 'Marathi', 'Regional', 'Maharashtra', 'India', 'Maharashtra', 'Mumbai', 'https://www.tv9marathi.com', 'https://www.youtube.com/@TV9Marathi', 340),
    ('ZEE_24_KALAK', 'Zee 24 Kalak', 'Gujarati', 'Regional', 'Gujarat', 'India', 'Gujarat', 'Ahmedabad', 'https://zeenews.india.com/gujarati', 'https://www.youtube.com/@Zee24Kalak', 350),
    ('ABP_ANANDA', 'ABP Ananda', 'Bengali', 'Regional', 'West Bengal', 'India', 'West Bengal', 'Kolkata', 'https://bengali.abplive.com', 'https://www.youtube.com/@abpananda', 360),
    ('KANAK_NEWS', 'Kanak News', 'Odia', 'Regional', 'Odisha', 'India', 'Odisha', 'Bhubaneswar', 'https://kanaknews.com', 'https://www.youtube.com/@KanakNewsOdisha', 370)
)
INSERT INTO public.news_channels
(
    channel_code,
    channel_name,
    language,
    category,
    region,
    country,
    state,
    city,
    description,
    logo_url,
    website_url,
    live_url,
    sort_order,
    is_active,
    created_at,
    updated_at
)
SELECT
    channel_code,
    channel_name,
    language,
    category,
    region,
    country,
    state_name,
    city,
    'Official news channel feed for ' || channel_name || '.',
    'https://www.google.com/s2/favicons?sz=128&domain_url=' || website_url,
    website_url,
    live_url,
    sort_order,
    true,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
FROM seed
ON CONFLICT (channel_code) DO UPDATE
SET
    channel_name = EXCLUDED.channel_name,
    language = EXCLUDED.language,
    category = EXCLUDED.category,
    region = EXCLUDED.region,
    country = EXCLUDED.country,
    state = EXCLUDED.state,
    city = EXCLUDED.city,
    description = EXCLUDED.description,
    logo_url = EXCLUDED.logo_url,
    website_url = EXCLUDED.website_url,
    live_url = EXCLUDED.live_url,
    sort_order = EXCLUDED.sort_order,
    is_active = true,
    updated_at = CURRENT_TIMESTAMP;

DELETE FROM public.news_broadcast_slots
WHERE starts_at >= date_trunc('day', CURRENT_TIMESTAMP)
AND starts_at < date '2027-01-01';

WITH dates AS (
    SELECT generate_series(CURRENT_DATE, date '2026-12-31', interval '1 day')::date AS slot_date
),
slot_defs(slot_no, program_title, program_type, slot_time, minutes) AS (
    VALUES
    (1, 'Morning Bulletin', 'Headlines', time '07:00', 60),
    (2, 'Market Watch', 'Business', time '09:30', 45),
    (3, 'Afternoon Update', 'Live Update', time '13:00', 60),
    (4, 'Prime Debate', 'Debate', time '20:00', 60),
    (5, 'Late News Wrap', 'Recap', time '22:30', 45)
),
active_channels AS (
    SELECT id, channel_name, row_number() OVER (ORDER BY sort_order, channel_name) AS rn, count(*) OVER () AS total
    FROM public.news_channels
    WHERE is_active = true
),
expanded AS (
    SELECT
        d.slot_date,
        s.slot_no,
        s.program_title,
        s.program_type,
        s.slot_time,
        s.minutes,
        row_number() OVER (ORDER BY d.slot_date) AS day_index
    FROM dates d
    CROSS JOIN slot_defs s
)
INSERT INTO public.news_broadcast_slots
(
    channel_id,
    program_title,
    program_type,
    starts_at,
    ends_at,
    is_live,
    created_at
)
SELECT
    c.id,
    c.channel_name || ' - ' || e.program_title,
    e.program_type,
    ((e.slot_date + e.slot_time) AT TIME ZONE 'Asia/Kolkata'),
    ((e.slot_date + e.slot_time) AT TIME ZONE 'Asia/Kolkata') + make_interval(mins => e.minutes),
    true,
    CURRENT_TIMESTAMP
FROM expanded e
JOIN active_channels c ON c.rn = (((e.day_index + e.slot_no - 2) % c.total) + 1)
ORDER BY e.slot_date, e.slot_no;
