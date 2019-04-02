using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartSql;

namespace SkyApm.Sample.SmartSql.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class SmartSqlController : ControllerBase
    {
        private readonly ISqlMapper _sqlMapper;

        public SmartSqlController(ISqlMapper sqlMapper)
        {
            _sqlMapper = sqlMapper;
        }
        [HttpGet]
        public Guid QueryError()
        {
            return _sqlMapper.QuerySingle<Guid>(new RequestContext
            {
                RealSql = "Error Sql"
            });
        }
        [HttpGet]
        public async Task<IEnumerable<dynamic>> QueryAsync()
        {
            return await _sqlMapper.QueryAsync<dynamic>(new RequestContext
            {
                RealSql = "SELECT T.* From T_User T Limit 100"
            });
        }
        [HttpGet]
        public IEnumerable<dynamic> Query()
        {
            return _sqlMapper.Query<dynamic>(new RequestContext
            {
                RealSql = "SELECT T.* From T_User T Limit 100"
            });
        }

        [HttpGet]
        public IEnumerable<dynamic> Transaction()
        {
            try
            {
                _sqlMapper.BeginTransaction();

                var list = _sqlMapper.Query<dynamic>(new RequestContext
                {
                    RealSql = "SELECT T.* From T_User T Limit 100"
                });
                _sqlMapper.CommitTransaction();
                return list;
            }
            catch (Exception ex)
            {
                _sqlMapper.RollbackTransaction();
                throw;
            }
        }
        [HttpGet]
        public IEnumerable<dynamic> TransactionError()
        {
            try
            {
                _sqlMapper.BeginTransaction();

                var list = _sqlMapper.Query<dynamic>(new RequestContext
                {
                    RealSql = "Error Sql"
                });
                _sqlMapper.CommitTransaction();
                return list;
            }
            catch (Exception ex)
            {
                _sqlMapper.RollbackTransaction();
                throw;
            }
        }
    }
}