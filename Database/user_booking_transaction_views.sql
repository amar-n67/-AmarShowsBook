DROP VIEW IF EXISTS public.vw_booking_complete_details;
DROP VIEW IF EXISTS public.vw_booking_transaction_summary;

CREATE OR REPLACE VIEW public.vw_booking_complete_details AS
SELECT
    b.id AS booking_id,
    b.booking_ref,
    u."Id" AS user_id,
    u."Name" AS user_name,
    u."Email" AS user_email,
    ss."Type" AS show_type,
    COALESCE(m."Title", st."Title", ls."Title") AS show_title,
    COALESCE(l."Area", 'N/A'::text) AS location_name,
    ss."StartTime" AS start_time,
    COALESCE(seat_list.seat_numbers, 'N/A'::text) AS seat_numbers,
    COALESCE(b.booking_status, 'PENDING'::character varying) AS booking_status,
    COALESCE(b.payment_status, 'PENDING'::character varying) AS payment_status,
    b.total_tickets,
    b.total_amount,
    b.tax_amount,
    b.discount_amount,
    b.payable_amount,
    COALESCE(tx.transaction_ref::text, bt."TransactionRef"::text, 'N/A'::text) AS transaction_ref,
    COALESCE(tx.payment_method::text, bt."PaymentMethod"::text, 'N/A'::text) AS payment_method,
    COALESCE(tx.gateway_name::text, CASE WHEN bt."Id" IS NOT NULL THEN 'DUMMY_GATEWAY' END, 'N/A'::text) AS gateway_name,
    COALESCE(tx.status::text, bt."PaymentStatus"::text, 'PENDING'::text) AS transaction_status,
    b.booked_at,
    b.confirmed_at,
    b.cancelled_at,
    b.created_at,
    CASE
        WHEN b.booking_status::text = 'FAILED'::text THEN 1
        ELSE 0
    END AS is_error
FROM bookings b
JOIN "Users" u ON b.user_id = u."Id"
JOIN "ShowSchedules" ss ON b.schedule_id = ss."Id"
LEFT JOIN "Movies" m ON ss."MovieId" = m."Id"
LEFT JOIN "StandupShows" st ON ss."StandupShowId" = st."Id"
LEFT JOIN "LiveStreams" ls ON ss."LiveStreamId" = ls."Id"
LEFT JOIN "Locations" l ON ss."LocationId" = l."Id"
LEFT JOIN LATERAL (
    SELECT string_agg(tk.seat_number, ', ' ORDER BY tk.seat_number) AS seat_numbers
    FROM tickets tk
    WHERE tk.booking_id = b.id
) seat_list ON true
LEFT JOIN LATERAL (
    SELECT t.*
    FROM transactions t
    WHERE t.id = b.transaction_id
       OR t.booking_id = b.id
    ORDER BY
        CASE WHEN t.id = b.transaction_id THEN 0 ELSE 1 END,
        t.completed_at DESC NULLS LAST,
        t.created_at DESC NULLS LAST
    LIMIT 1
) tx ON true
LEFT JOIN LATERAL (
    SELECT bt_inner.*
    FROM booking_transactions bt_inner
    WHERE bt_inner."BookingId" = b.id
    ORDER BY bt_inner."PaidAt" DESC NULLS LAST, bt_inner."CreatedAt" DESC NULLS LAST
    LIMIT 1
) bt ON true;

ALTER TABLE public.vw_booking_complete_details OWNER TO postgres;

CREATE OR REPLACE VIEW public.vw_booking_transaction_summary AS
SELECT
    b.id AS booking_id,
    COALESCE(b.booking_ref, ''::character varying) AS booking_ref,
    u."Id" AS user_id,
    COALESCE(u."Name", ''::text) AS user_name,
    COALESCE(u."Email", ''::text) AS user_email,
    COALESCE(s."Type", ''::text) AS show_type,
    COALESCE(m."Title", ss."Title", ls."Title", ''::text) AS show_title,
    COALESCE(b.booking_status, ''::character varying) AS booking_status,
    COALESCE(tx.id, bt."Id") AS transaction_id,
    COALESCE(tx.transaction_ref::text, bt."TransactionRef"::text, ''::text) AS transaction_ref,
    COALESCE(tx.payment_method::text, bt."PaymentMethod"::text, ''::text) AS payment_method,
    COALESCE(tx.amount, bt."Amount", 0::numeric) AS transaction_amount,
    COALESCE(tx.currency, 'INR'::character varying) AS currency,
    COALESCE(tx.status::text, bt."PaymentStatus"::text, ''::text) AS transaction_status,
    COALESCE(tx.gateway_name::text, CASE WHEN bt."Id" IS NOT NULL THEN 'DUMMY_GATEWAY' END, ''::text) AS gateway_name,
    COALESCE(tx.failure_reason, ''::text) AS failure_reason,
    CASE
        WHEN lower(COALESCE(tx.status::text, bt."PaymentStatus"::text, ''::text)) = 'failed'::text THEN 1
        ELSE 0
    END AS is_payment_error,
    COALESCE(b.total_amount, 0::numeric) AS total_amount,
    b.created_at AS booking_created_at,
    COALESCE(tx.completed_at, bt."PaidAt") AS completed_at
FROM bookings b
LEFT JOIN LATERAL (
    SELECT t.*
    FROM transactions t
    WHERE t.id = b.transaction_id
       OR t.booking_id = b.id
    ORDER BY
        CASE WHEN t.id = b.transaction_id THEN 0 ELSE 1 END,
        t.completed_at DESC NULLS LAST,
        t.created_at DESC NULLS LAST
    LIMIT 1
) tx ON true
LEFT JOIN LATERAL (
    SELECT bt_inner.*
    FROM booking_transactions bt_inner
    WHERE bt_inner."BookingId" = b.id
    ORDER BY bt_inner."PaidAt" DESC NULLS LAST, bt_inner."CreatedAt" DESC NULLS LAST
    LIMIT 1
) bt ON true
LEFT JOIN "Users" u ON b.user_id = u."Id"
LEFT JOIN "ShowSchedules" s ON b.schedule_id = s."Id"
LEFT JOIN "Movies" m ON s."MovieId" = m."Id"
LEFT JOIN "StandupShows" ss ON s."StandupShowId" = ss."Id"
LEFT JOIN "LiveStreams" ls ON s."LiveStreamId" = ls."Id";

ALTER TABLE public.vw_booking_transaction_summary OWNER TO postgres;
