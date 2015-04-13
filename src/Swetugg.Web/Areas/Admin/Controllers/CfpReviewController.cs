﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Swetugg.Web.Controllers;
using Swetugg.Web.Models;

namespace Swetugg.Web.Areas.Admin.Controllers
{
    public class CfpReviewController : ConferenceAdminControllerBase
    {
        public CfpReviewController(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        [Route("{conferenceSlug}/cfp")]
        public async Task<ActionResult> Index()
        {
            var speakers = await dbContext.CfpSpeakers.Include(s => s.Sessions).ToListAsync();

            ViewBag.Conference = Conference;
            
            return View(speakers);
        }

        [Route("{conferenceSlug}/cfp/speaker/{id:int}")]
        public async Task<ViewResult> Speaker(int id)
        {
            var speaker = await dbContext.CfpSpeakers.Include(s => s.Sessions.Select(se => se.Session)).Include(s => s.Speaker).SingleAsync(s => s.Id == id);

            ViewBag.Conference = Conference;

            return View(speaker);
        }

        [Route("{conferenceSlug}/cfp/session/{id:int}")]
        public async Task<ViewResult> Session(int id)
        {
            var session = await dbContext.CfpSessions.Include(s => s.Speaker).Include(s => s.Session).SingleAsync(s => s.Id == id);

            ViewBag.Conference = Conference;
            ViewBag.Speaker = session.Speaker;

            return View(session);
        }

        [HttpPost]
        [Route("{conferenceSlug}/cfp/speaker/{id:int}/promote")]
        public async Task<RedirectToRouteResult> Promote(int id)
        {
            var cfpSpeaker = await dbContext.CfpSpeakers.SingleAsync(s => s.Id == id);

            var conferenceId = ConferenceId;

            var speaker = new Speaker()
            {
                ConferenceId = conferenceId,
                Name = cfpSpeaker.Name,
                Company = cfpSpeaker.Company,
                Bio = cfpSpeaker.Bio,
                Slug = cfpSpeaker.Name.Slugify(),
                Web = cfpSpeaker.Web,
                Twitter = cfpSpeaker.Twitter,
                GitHub = cfpSpeaker.GitHub
            };
            cfpSpeaker.Speaker = speaker;

            dbContext.Entry(speaker).State = EntityState.Added;

            await dbContext.SaveChangesAsync();

            return RedirectToAction("Speaker", new { id });
        }

        [HttpPost]
        [Route("{conferenceSlug}/cfp/speaker/{id:int}/promote-sessions")]
        public async Task<ActionResult> PromoteSessions(int id, List<int> sessionIds)
        {
            var cfpSpeaker = await dbContext.CfpSpeakers.Include(s => s.Sessions).Include(s => s.Speaker.Sessions).SingleAsync(s => s.Id == id);
            var cfpSessions = cfpSpeaker.Sessions;
            
            var speaker = cfpSpeaker.Speaker;

            var conferenceId = ConferenceId;
            if (sessionIds != null && sessionIds.Any())
            {
                foreach (var s in cfpSessions.Where(s => sessionIds.Contains(s.Id)))
                {
                    var session = new Session()
                    {
                        ConferenceId = conferenceId,
                        Name = s.Name,
                        Slug = s.Name.Slugify(),
                        Description = s.Description,
                    };
                    s.Session = session;
                    speaker.Sessions.Add(new SessionSpeaker()
                    {
                        Session = session
                    });
                }
                await dbContext.SaveChangesAsync();
            }

            return RedirectToAction("Speaker", new { id });
        }

        [HttpPost]
        [Route("{conferenceSlug}/cfp/speaker/{id:int}/update")]
        public async Task<ActionResult> Update(int id)
        {
            var cfpSpeaker = await dbContext.CfpSpeakers.Include(s => s.Sessions.Select(se => se.Session)).Include(s => s.Speaker).SingleAsync(s => s.Id == id);
            var cfpSessions = cfpSpeaker.Sessions;

            var speaker = cfpSpeaker.Speaker;
            if (speaker != null)
            {
                speaker.Name = cfpSpeaker.Name;
                speaker.Bio = cfpSpeaker.Bio;
                speaker.Web = cfpSpeaker.Web;
                speaker.Twitter = cfpSpeaker.Twitter;
                speaker.Company = cfpSpeaker.Company;
                speaker.GitHub = cfpSpeaker.GitHub;
            }
            foreach (var cfpSession in cfpSessions)
            {
                var session = cfpSession.Session;
                if (session != null)
                {
                    session.Name = cfpSession.Name;
                    session.Description = cfpSession.Description;
                }
            }
            await dbContext.SaveChangesAsync();

            return RedirectToAction("Speaker", new {id});
        }
    }
}