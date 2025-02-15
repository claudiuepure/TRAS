﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TripleStore.Stardog;
using ViewModels;

namespace TRAS.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        //
        // GET: /UserProfile/
        public ActionResult Index()
        {
            PersonViewModel personVM = null;
            var username = User.Identity.Name;
            if (!string.IsNullOrEmpty(username))
            {
                var db = StardogDb.GetInstance();
                personVM = db.GetPerson(username);
            }
            return View(personVM);
        }

        public ActionResult Edit(string id)
        {
            var username = User.Identity.Name;
            if (!id.Equals(username))
            {
                return RedirectToAction("Profile", null, new { id = username });
            }
            PersonViewModel personVM = null;
            if (!string.IsNullOrEmpty(id))
            {
                var db = StardogDb.GetInstance();
                personVM = db.GetPerson(id);
            }
            return View(personVM);
        }

        [HttpPost]
        public ActionResult Update(PersonViewModel personVM) 
        {
            var db = StardogDb.GetInstance();
            db.CreateOrUpdatePerson(personVM);
            return RedirectToAction("Index", "UserProfile");
        }

        public ActionResult Profile(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index", "Home");
            }
            PersonViewModel personVM = null;
            if (!string.IsNullOrEmpty(id))
            {
                var db = StardogDb.GetInstance();
                personVM = db.GetPerson(id);
            }
            return View(personVM);
        }
	}
}