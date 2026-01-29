using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Exceptions
{
    internal class UserReviewNotFoundException : NotFoundException
    {
        public UserReviewNotFoundException(int id) : base($"{id} id'sine sahip değerlendirme bulunamadı.")
        {
        }
    }
}
