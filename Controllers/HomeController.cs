﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LoginReg.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LoginReg.Controllers
{
    public class HomeController : Controller
    {
        private LoginRegContext db;

        private int? uid
        {
            get
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // HttpContext.Session.SetInt32("UserId", 3); ////////////////////////////////////////////////////////////////////delete/comment out 4 deployment!
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                return HttpContext.Session.GetInt32("UserId");
            }
        }

        private bool isLoggedIn
        {
            get
            {
                return uid != null;
            }
        }
        public HomeController(LoginRegContext context)
        {
            db = context;
        }

        [HttpGet("")]
        public IActionResult LoginRegPage()
        {
            
            return View("Index");
        }


        [HttpGet("/loginpage")]
        public IActionResult LoginPage()
        {
            
            return View("_Login");
        }
        [HttpGet("/registerpage")]
        public IActionResult RegisterPage()
        {
            
            return View("_Register");
        }

        public IActionResult Register(User newUser)
        {
            if (ModelState.IsValid)
            {
                //verify email not in use
                bool emailInUse = db.Users.Any(u => u.Email == newUser.Email);
                if(emailInUse)
                {
                    ModelState.AddModelError("Email", "This Username/Email is already taken");
                    Console.WriteLine("ougabooga");
                }
            }

            /*
            check to make sure the conditions above invalidate the model state 
            and return all the errors at once
            */
            if (ModelState.IsValid == false)
            {
                /* 
                Send back to the page with the form so error messages are
                displayed with the filled in input data.
                */
                return View("Index");
            }


            //hash password
            PasswordHasher<User> hasher = new PasswordHasher<User>();
            newUser.Password = hasher.HashPassword(newUser, newUser.Password);
            /* 
            This Add method auto generates SQL code:
            INSERT INTO ModelName (Custom Properties, CreatedAt, UpdatedAt)
            */
            db.Users.Add(newUser); 
            // db doesn't update until we run save changes
            // After SaveChanges, our object now has it's ModelNameId from the db (autogenerated!).
            db.SaveChanges();
            
            HttpContext.Session.SetInt32("UserId", newUser.UserId);
            HttpContext.Session.SetString("FullName", newUser.FullName());
            return RedirectToAction("Dashboard");
        }


        [HttpPost("Login")]
        public IActionResult Login(LoginUser LoginUser)
        {
            string genericErrMsg = "Invalid Email or Password";

            if (ModelState.IsValid == false)
            {
                /* 
                Send back to the page with the form so error messages are
                displayed with the filled in input data.
                */
                return View("Index");
            }

            //find user attached to the email address used to log in
            User LoggedInUser = db.Users.FirstOrDefault(p => p.Email == LoginUser.LoginEmail);

            if (LoggedInUser == null)
            {
                ModelState.AddModelError("LoginEmail", genericErrMsg);
                Console.WriteLine(new String('*', 30) + "Login: Email not found");

                return View("Index");
            }
            
            //User found b/c the above did not return
            PasswordHasher<LoginUser> hasher = new PasswordHasher<LoginUser>();
            PasswordVerificationResult ComparePasswords = hasher.VerifyHashedPassword(LoginUser, LoggedInUser.Password, LoginUser.LoginPassword);

            if(ComparePasswords == 0)
            {
                ModelState.AddModelError("LoginEmail", genericErrMsg);
                return View("Index");
            }
            HttpContext.Session.SetInt32("UserId", LoggedInUser.UserId);
            HttpContext.Session.SetString("FullName", LoggedInUser.FullName());

            return RedirectToAction("Dashboard");
        }
        [HttpGet("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

//============================================================================================================================================================================
//============================================================================================================================================================================
//============================================================================================================================================================================

        [HttpGet("/dashboard")]
        public IActionResult Dashboard()
        {
            Console.WriteLine(isLoggedIn);
            if(isLoggedIn == false)
            {
                Console.WriteLine("++++++++++++++++++++++++++++++++++++++++");
                Console.WriteLine("supersecrethacker");
                return RedirectToAction("LoginRegPage", "Home");
            }
            List<Trip> TripsImOn = db.Trips.Include(trip => trip.Tourists).ThenInclude(rel => rel.UserTourist).Include(Trip=> Trip.Guide).Where(trip => trip.GuideId == uid || trip.Tourists.Any(rel =>rel.UserTouristId == uid)).OrderByDescending(p => p.CreatedAt).ToList();
            ViewBag.TripsJoined = TripsImOn;

            List<Trip> TripsImNotOn = db.Trips.Include(trip => trip.Tourists).ThenInclude(rel => rel.UserTourist).Include(Trip=> Trip.Guide).Where(trip => trip.GuideId != uid ).OrderByDescending(p => p.CreatedAt).ToList();
            ViewBag.TripsNotJoined = TripsImNotOn;

            User LoggedInUser = db.Users.Include(u => u.GuidedTrips).ThenInclude( trips => trips.UserTourist).Include(u => u.MyTrips).FirstOrDefault(u => u.UserId == uid);

            return View(LoggedInUser);
        }



        [HttpGet("/trip/new")]
        public IActionResult NewTrip()
        {
            if(!isLoggedIn)
                {
                    return RedirectToAction("LoginRegPage", "Home");
                }
            return View();
        }

        [HttpPost("/trip/create")]
        public IActionResult CreateTrip(Trip newTrip)
        {
            if(!isLoggedIn)
            {
                return RedirectToAction("LoginRegPage", "Home");
            }
            if (!ModelState.IsValid)
            {
                // To display validation errors.
                return View("NewTrip");
            }


            // WILL GET THIS ERROR if FK is not assigned:
            // "foreign key constraint fails"
            int? id = HttpContext.Session.GetInt32("UserId");
            Console.WriteLine("************************************");
            Console.WriteLine("************************************");
            Console.WriteLine("************************************");
            Console.WriteLine(HttpContext.Session.GetInt32("UserId"));
            newTrip.GuideId = (int)uid;
            db.Trips.Add(newTrip);
            db.SaveChanges(); 

            /* 
            WHENEVER REDIRECTING to a method that has params, you must pass in
            a 'new' dictionary: new { paramName = valueForParam }
            left matches argument in TripDetail => right matches value from this function/method ie:
            */
            return RedirectToAction("TripDetail", new { tripId = newTrip.TripId });
        }

        [HttpGet("/trip/{tripId}")]
        public IActionResult TripDetail(int tripId)
        {
            if(!isLoggedIn)
            {
                return RedirectToAction("LoginRegPage", "Home");
            }

            Trip trip = db.Trips.Include(trip => trip.Guide).Include(trip => trip.Tourists).ThenInclude(g =>g.UserTourist).FirstOrDefault(p => p.TripId == tripId);
            if (trip == null)
            {
                return RedirectToAction("Dashboard");
            }
            
            ViewBag.thisTrip = trip;
            return View("TripDetail", trip);
        }


        [HttpPost("/trip/link/{tripId}")]
        public IActionResult LinkRSVP(int tripId)
        {
            if(!isLoggedIn)
            {
                return RedirectToAction("LoginRegPage", "Home");
            }
            UTRel existingResponse = db.UTRels.FirstOrDefault(rsvp => rsvp.UserTouristId == (int)uid && rsvp.GuidedTripId == tripId);

            if (existingResponse != null)
            {
                Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++");
                Console.WriteLine("unlink");
                db.UTRels.Remove(existingResponse);
            }
            else
            {
                UTRel newRSVP = new UTRel()
                {
                    GuidedTripId = tripId,
                    UserTouristId = (int)uid,
                };
                db.UTRels.Add(newRSVP);
            }
            
            db.SaveChanges();

            return RedirectToAction("Dashboard");
        }


        [HttpGet("/trip/edit/{tripId}")]
        public IActionResult EditTrip(int tripId)
        {
            Trip trip = db.Trips.Include(t => t.Guide.FirstName).FirstOrDefault(p => p.TripId == tripId);

            return View("EditTrip", trip);
        }

        [HttpPost("/trip/update/{tripId}")]

        public IActionResult UpdateTrip(Trip newTrip, int tripId)
        {
            if(!isLoggedIn)
            {
                return RedirectToAction("LoginRegPage", "Home");
            }
            if (ModelState.IsValid == false)
            {
                /* 
                Send back to the page with the form so error messages are
                displayed with the filled in input data.
                */
                return View("Edit");
            }

            Trip trip = db.Trips.FirstOrDefault(p => p.TripId == tripId);
            
            trip.Destination = newTrip.Destination;
            trip.StartDate = newTrip.StartDate;
            trip.EndDate = newTrip.EndDate;
            trip.Plan = newTrip.Plan;
            trip.GuideId = (int)uid;

            trip.UpdatedAt = DateTime.Now;

            db.Trips.Update(trip);
            db.SaveChanges();

            return RedirectToAction("Dashboard");
        }



        [HttpPost("/trip/delete/{tripId}")]
        public IActionResult Delete (int tripId)
        {
            if(!isLoggedIn)
            {
                return RedirectToAction("LoginRegPage", "Home");
            }
            Trip trip = db.Trips.FirstOrDefault(p => p.TripId == tripId);

            if (trip != null)
            {
                db.Trips.Remove(trip);
                db.SaveChanges();
            }
            return RedirectToAction("Dashboard");
        }





    }
}
