namespace AmarShowsBook.Models.Admin
{
    public class DeletedUser
    {
        public long id { get; set; }

        public long original_user_id { get; set; }

        public string? name { get; set; }

        public string? email { get; set; }

        public string? mobile { get; set; }

        public string? address { get; set; }

        public string? country { get; set; }

        public string? state { get; set; }

        public string? district { get; set; }

        public string? pincode { get; set; }

        public string? language { get; set; }

        public string? genre { get; set; }

        public string? profile_image_path { get; set; }

        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        public DateTime deleted_at { get; set; }

        public string? deleted_by { get; set; }

        public DateTime? revoke_at { get; set; }

        public string? revoked_by { get; set; }

        public bool is_revoked { get; set; }
    }
}