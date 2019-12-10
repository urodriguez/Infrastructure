﻿using System;
using System.Collections.Generic;
using System.Linq;
using Auditing.Domain;
using Auditing.Dtos;
using Auditing.Infrastructure.Persistence;
using Infrastructure.CrossCutting.LogService;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Auditing.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuditsController : ControllerBase
    {
        private readonly AuditingDbContext _auditingDbContext;
        private readonly ILogService _logService;
        private readonly IChangesService _changesService;

        public AuditsController(AuditingDbContext auditingDbContext, ILogService logService, IChangesService changesService)
        {
            _auditingDbContext = auditingDbContext;
            _logService = logService;
            _changesService = changesService;
        }

        [HttpGet]
        public IActionResult Get(int page = 0, int pageSize = 10, string sortBy = "creationDate", string sortOrder = "desc", DateTime? sinceDate = null, DateTime? toDate = null)
        {
            _logService.LogInfoMessage($"AuditController.Get | INPUT | page={page} - pageSize={pageSize} - sortBy={sortBy} - sortOrder={sortOrder} - sinceDate={sinceDate} - toDate={toDate}");

            if (sortOrder != "asc" && sortOrder != "desc") return BadRequest($"Invalid sortOrder = {sortBy} parameter. Only 'asc' and 'desc' are allowed");

            IQueryable<Audit> searchedAudits;
            if (sinceDate == null && toDate == null)
            {
                sinceDate = DateTime.UtcNow.AddDays(-30);
                searchedAudits = _auditingDbContext.Audits.Where(sl => sinceDate <= sl.CreationDate);
            }
            else if (sinceDate != null && toDate != null)
            {
                var toDateAtEnd = toDate.Value.AddHours(23).AddMinutes(59).AddSeconds(59);
                searchedAudits = _auditingDbContext.Audits.Where(sl => sinceDate <= sl.CreationDate && sl.CreationDate <= toDateAtEnd);
            }
            else
            {
                var toDateAtEnd = toDate.Value.AddHours(23).AddMinutes(59).AddSeconds(59);
                searchedAudits = _auditingDbContext.Audits.Where(pl => sinceDate == null && toDate != null ? pl.CreationDate <= toDateAtEnd : sinceDate <= pl.CreationDate);
            }
            _logService.LogInfoMessage($"AuditController.Get | Searching done");

            IQueryable<Audit> sortedAudits;
            switch (sortBy)
            {
                case "application":
                    sortedAudits = sortOrder == "asc" ? searchedAudits.OrderBy(pl => pl.Application) : searchedAudits.OrderByDescending(pl => pl.Application);
                    break;
                case "user":
                    sortedAudits = sortOrder == "asc" ? searchedAudits.OrderBy(pl => pl.User) : searchedAudits.OrderByDescending(pl => pl.User);
                    break;
                case "action":
                    sortedAudits = sortOrder == "asc" ? searchedAudits.OrderBy(pl => pl.Action) : searchedAudits.OrderByDescending(pl => pl.Action);
                    break;
                case "creationDate":
                    sortedAudits = sortOrder == "asc" ? searchedAudits.OrderBy(pl => pl.CreationDate) : searchedAudits.OrderByDescending(pl => pl.CreationDate);
                    break;

                default:
                    return BadRequest($"Invalid sortBy = {sortBy} parameter");
            }
            _logService.LogInfoMessage($"AuditController.Get | Sorting done");

            var pagedAudits = sortedAudits.Skip(page * pageSize).Take(pageSize);
            _logService.LogInfoMessage($"AuditController.Get | Paging done");

            return Ok(pagedAudits.Select(pa => new AuditDtoGet
            {
                User = pa.User,
                Changes = JsonConvert.DeserializeObject<IEnumerable<EntityChange>>(pa.Changes),
                Action = pa.Action,
                CreationDate = pa.CreationDate
            }).ToList());
        }

        [HttpPost]
        public IActionResult Audit([FromBody] AuditDtoPost auditDto)
        {
            try
            {
                _logService.LogInfoMessage($"AuditController.Audit | INPUT | entity={auditDto.EntityName} - application={auditDto.Application} - user={auditDto.User} - action={auditDto.Action}");

                var audit = new Audit(auditDto.Application, auditDto.User, auditDto.EntityName, auditDto.Action);

                audit.ValidateSerializedEntity(auditDto.Entity);
                audit.ValidateOldSerializedEntity(auditDto.OldEntity, auditDto.Action);

                var entityChanges = _changesService.GetChanges(auditDto.Entity, auditDto.OldEntity, auditDto.Action);
                audit.Changes = JsonConvert.SerializeObject(entityChanges, Formatting.Indented);

                _auditingDbContext.Audits.Add(audit);
                _auditingDbContext.SaveChanges();

                _logService.LogInfoMessage($"AuditController.Audit | Entity saved | audit.Id={audit.Id}");

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}