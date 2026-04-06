using dataproduct.api.Models;
using Microsoft.EntityFrameworkCore;

namespace dataproduct.api.Repositories
{
    public class BBGN_ThepLongRepository : IBBGN_ThepLongRepository
    {
        private readonly ProductFormContext _context;

        public BBGN_ThepLongRepository(ProductFormContext context)
        {
            _context = context;
        }
    }
}
