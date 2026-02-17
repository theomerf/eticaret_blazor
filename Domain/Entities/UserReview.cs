using Domain.Exceptions;

namespace Domain.Entities
{
    public class UserReview : SoftDeletableEntity
    {
        public int UserReviewId { get; set; }
        public int Rating { get; set; }
        public string? ReviewTitle { get; set; }
        public string? ReviewText { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;
        public DateTime ReviewUpdateDate { get; set; } = DateTime.UtcNow;
        public string? ReviewPictureUrl { get; set; }

        public bool IsApproved { get; set; } = false;
        public bool IsFeatured { get; set; } = false;
        public string ReviewerName { get; set; } = null!;

        public ICollection<UserReviewVote> Votes { get; set; } = [];
        public int HelpfulCount { get; set; } = 0;
        public int NotHelpfulCount { get; set; } = 0;

        public string UserId { get; set; } = null!;
        public User? User { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        #region Validation Methods

        public void ValidateForCreation()
        {
            if (Rating < 1 || Rating > 5)
            {
                throw new UserReviewValidationException("Değerlendirme puanı 1 ile 5 arasında olmalıdır.");
            }

            if (!string.IsNullOrWhiteSpace(ReviewTitle))
            {
                if (ReviewTitle.Length < 3 || ReviewTitle.Length > 200)
                {
                    throw new UserReviewValidationException("Değerlendirme başlığı 3-200 karakter arasında olmalıdır.");
                }
            }

            if (!string.IsNullOrWhiteSpace(ReviewText))
            {
                if (ReviewText.Length < 5 || ReviewText.Length > 2000)
                {
                    throw new UserReviewValidationException("Değerlendirme metni 5-2000 karakter arasında olmalıdır.");
                }
            }

            if (!string.IsNullOrWhiteSpace(ReviewPictureUrl))
            {
                if (ReviewPictureUrl.Length > 500)
                {
                    throw new UserReviewValidationException("Resim URL'si en fazla 500 karakter olabilir.");
                }

                /*if (!Uri.TryCreate(ReviewPictureUrl, UriKind.Absolute, out _))
                {
                    throw new UserReviewValidationException("Geçerli bir resim URL'si giriniz.");
                }*/
            }

            if (ProductId <= 0)
            {
                throw new UserReviewValidationException("Geçerli bir ürün ID'si giriniz.");
            }

            if (string.IsNullOrWhiteSpace(UserId))
            {
                throw new UserReviewValidationException("Kullanıcı ID'si boş olamaz.");
            }
        }

        public void ValidateForUpdate()
        {
            ValidateForCreation();
        }

        #endregion

        #region Business Logic Methods

        public void MarkAsHelpful()
        {
            HelpfulCount++;
        }

        public void MarkAsNotHelpful()
        {
            NotHelpfulCount++;
        }

        public void Approve()
        {
            IsApproved = true;
        }

        public void Unapprove()
        {
            IsApproved = false;
        }

        public void ToggleFeatured()
        {
            IsFeatured = !IsFeatured;
        }

        public int UpdateReview()
        {
            var oldRating = Rating;
            ReviewUpdateDate = DateTime.UtcNow;
            
            if (IsApproved)
            {
                IsApproved = false;
                return oldRating;
            }
            
            return 0;
        }

        public void SoftDelete(string deletedByUserId)
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            DeletedByUserId = deletedByUserId;
        }

        #endregion
    }
}
