using EskaCMS.Core.Entities;
using EskaCMS.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Core.Enums.GeneralEnums;

namespace EskaCMS.Core.Services
{
    public class dcmsLogService : IdcmsLogService
    {
        private readonly IRepository<DCMSLog> _dcmsLogRepository;
        private readonly ILogger _logger;
        public dcmsLogService(IRepository<DCMSLog> dcmsLogRepository, ILoggerFactory loggerFactory)
        {
            _dcmsLogRepository = dcmsLogRepository;
            _logger = loggerFactory.CreateLogger<dcmsLogService>();
        }

        public async Task AddInfo(long? UserId, string Module, string Category, long EventId,string EventName,string Scope,long? ParentId,string Message,string AdditionalInfo,long? SiteId )
        {
            try
            {
                DCMSLog objlog = new DCMSLog();

                objlog.AdditionInfo = AdditionalInfo;
                objlog.Category = Category;
                objlog.EventId = EventId;
                objlog.UserId = UserId;
                objlog.EventName = EventName;
                objlog.LogType = LogTypes.Information;
                objlog.Message = Message;
                objlog.Module = Module;
                objlog.Scope = Scope;
                objlog.ParentId = ParentId;
                objlog.SiteId = SiteId;
                _dcmsLogRepository.Add(objlog);
                await _dcmsLogRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Faild to add Log/{GetType().Name}: {exc.Message}");
              
               
            }
        }
        public async Task AddError( string Module, string exception,string Message,string AdditionalInfo,long? SiteId)
        {
            try
            {
                DCMSLog objlog = new DCMSLog();

                objlog.AdditionInfo = AdditionalInfo;
                objlog.LogType= LogTypes.Error;
                objlog.SiteId = SiteId;
                objlog.Message = Message;
                objlog.Exception = exception;
                objlog.Module = Module;
                _dcmsLogRepository.Add(objlog);
                await _dcmsLogRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Faild to add error Log/{GetType().Name}: {exc.Message}");
               
            }
        }
        public async Task AddError(long? UserId, string Module,string Scope, string exception, string Message, string AdditionalInfo,long? SiteId)
        {
            try
            {
                DCMSLog objlog = new DCMSLog();

                objlog.AdditionInfo = AdditionalInfo;
                objlog.LogType = LogTypes.Error;
                objlog.Scope = Scope;
                objlog.UserId = UserId;
                objlog.Message = Message;
                objlog.Exception = exception;
                objlog.Module = Module;
                objlog.SiteId = SiteId;
                _dcmsLogRepository.Add(objlog);
                await _dcmsLogRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Faild to add error Log/{GetType().Name}: {exc.Message}");

            }
        }
        public async Task AddInfo(long? UserId, string Module,  string Message, string AdditionalInfo, long? SiteId)
        {
            try
            {
                DCMSLog objlog = new DCMSLog();
                objlog.LogType = LogTypes.Information;
                objlog.AdditionInfo = AdditionalInfo;
                objlog.UserId = UserId;
                objlog.Message = Message;
                objlog.Module = Module;
                objlog.SiteId = SiteId  ;
                _dcmsLogRepository.Add(objlog);
                await _dcmsLogRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Faild to add error Log/{GetType().Name}: {exc.Message}");

            }
        }

        public async Task AddInfo( string Module, string Message, string AdditionalInfo, long? SiteId)
        {
            try
            {
                DCMSLog objlog = new DCMSLog();
                objlog.LogType = LogTypes.Information;
                objlog.AdditionInfo = AdditionalInfo;
               
                objlog.Message = Message;
                objlog.Module = Module;
                objlog.SiteId = SiteId;
                _dcmsLogRepository.Add(objlog);
                await _dcmsLogRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Faild to add error Log/{GetType().Name}: {exc.Message}");

            }
        }

        public async Task AddWarning(long? UserId, string Module, string Message, string AdditionalInfo, long? SiteId)
        {
            try
            {
                DCMSLog objlog = new DCMSLog();
                objlog.LogType = LogTypes.Warning;
                objlog.AdditionInfo = AdditionalInfo;
                objlog.UserId = UserId;
                objlog.Message = Message;
                objlog.Module = Module;
                objlog.SiteId = SiteId;
                _dcmsLogRepository.Add(objlog);
                await _dcmsLogRepository.SaveChangesAsync();
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, $"Faild to add error Log/{GetType().Name}: {exc.Message}");

            }
        }
    }
}
