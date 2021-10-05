using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CourseDataAccess.Data.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T> Find(Guid id);

        Task<IList<T>> GetAll(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null);

        Task<T> GetFirstOrDefault(bool noTracking = true, Expression<Func<T, bool>> filter = null);

        Task Save(T entity);

        Task Update(T entity);
    }
}
