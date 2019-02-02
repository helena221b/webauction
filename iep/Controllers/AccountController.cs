using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using iep.Models;
using System.Net;
using System.Data.Entity;
using log4net;

using System.Collections.Generic;
using PagedList;
using System.Data;

using System.Net.Mail;

namespace iep.Controllers
{


    public class AccountController : Controller
    {
        private dbcontext db = new dbcontext();
        private static log4net.ILog Log { get; set; }
        ILog log = log4net.LogManager.GetLogger(typeof(ManageController));
        private String Hash(String password)
        {

            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            Byte[] originalBytes = System.Text.ASCIIEncoding.Default.GetBytes(password);
            Byte[] encodedBytes = md5.ComputeHash(originalBytes);

            return BitConverter.ToString(encodedBytes);
        }

        public ActionResult Register()
        {
            if (Session["User"] != null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            return View();
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register([Bind(Include = "FirstName, LastName, Email, Password")] User user)
        {
            if (ModelState.IsValid)
            {
                log.Info("Register User : " + user.Email);
                var korisnik = db.Users.Any(x => x.Email == user.Email);
                if (!korisnik)
                {
                    if (user.Password.Length < 8) {
                        ViewBag.Message = "Password must be at least 8 characters long!";
                            return View("Register");
                    }
                    user.Password = Hash(user.Password);
                    user.TokensNumber = 0;
                    db.Users.Add(user);
                    db.SaveChanges();
                    return RedirectToAction("Login");
                }
                else
                {
                    ViewBag.Message = "Email is already taken";
                    return View("Register");
                }

            }
            return View("Register");
            
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login([Bind(Include = "Email, Password")]User user)
        {

            
            user.Password = Hash(user.Password);
            var korisnik = db.Users.Any(x => x.Email == user.Email && x.Password == user.Password);
            if (korisnik)
            {
                var u = db.Users.First(x => x.Email == user.Email && x.Password == user.Password);
                if (user.Email == "admin@gmail.com")
                {
                    Session["Admin"] = true;
                    Session["AdminUser"] = u;
                    log.Info("Logged User Admin : " + user.Id);
                    return RedirectToAction("ReadyAuctions", "Manage");

                }
                else
                {
                    Session["Name"] = u.FirstName;
                    Session["User"] = u;
                    log.Info("Logged User : " + user.Id);
                    return RedirectToAction("SearchAuctions", "Manage");
                }  
            }
            else
            {
                ViewBag.Message = "No user matches this username and password";
                 return View("Login");

            }

        }
        public ActionResult Logout()
        {

            if (((User)Session["User"]) != null)
            {
                try
                {
                    Session.Clear();
                     log.Info("Logged off : " + ((User)Session["User"]).Id);
                }
                catch (Exception e)
                { }

            }

            if (((User)Session["AdminUser"]) != null)
            {
                try
                {
                    Session.Clear();
                    log.Info("Logged off : " + ((User)Session["User"]).Id);
                }
                catch (Exception e)
                { }

            }
            return RedirectToAction("Index", "Home");
        }
        public ActionResult Manage()
        {
            if (Session["Admin"] != null || Session["User"] == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            User user = db.Users.Find(((User)(Session["User"])).Id);
            if (user == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            return View(user);
        }
        public ActionResult Edit(int? id)
        {
            if (Session["Admin"] != null || Session["User"] == null || id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            User user = db.Users.Find(id);
            if (user == null)
                return HttpNotFound();
            else return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "FirstName, LastName, Email")] User user) {
           
            if(Session["Admin"]!=null || Session["User"]==null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            log.Info("Edit user " + ((User)Session["User"]).Id);
            var id = ((User)Session["User"]).Id;
            if (!db.Users.Any(x => x.Email == user.Email && x.Id != id))
            {
                user.Id = ((User)Session["User"]).Id;
                user.TokensNumber = ((User)Session["User"]).TokensNumber;
                user.Password = ((User)Session["User"]).Password;
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                ((User)Session["User"]).FirstName = user.FirstName;
                ((User)Session["User"]).LastName = user.LastName;
                ((User)Session["User"]).Email = user.Email;
                return RedirectToAction("Index", "Manage");
            }
            else {
                ViewBag.Message = "Email has already been taken";
                return View(user);
            }
        }
        public ActionResult ChangePassword() {
            if (Session["Admin"] != null || Session["User"] == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword([Bind(Include = "OldPassword, NewPassword, ConfirmPassword")]Password data) {
            if(Session["Admin"]!=null || Session["User"]==null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var user= db.Users.Find(((User)Session["User"]).Id);
            var currPass = ((User)Session["User"]).Password;
            if (Hash(data.OldPassword).Equals(currPass) && data.NewPassword.Length > 7 && data.ConfirmPassword.Length > 7) {
                if (data.NewPassword.Equals(data.ConfirmPassword))
                {
                    user.Password = Hash(data.NewPassword);
                    ((User)Session["User"]).Password = user.Password;
                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index", "Manage");
                }
                else {
                    ViewBag.Message = "New password and confirm password don't match";
                    return RedirectToAction("ChangePassword");
                }

            }
            else{
                ViewBag.Message = "Data are not valid. Please try again.";
                return RedirectToAction("ChangePassword");
            }
        }
        public ActionResult AllTokenOrders(int? page) {
            if (Session["Admin"] != null || Session["User"] == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var id = ((User)Session["User"]).Id;
            var orders = db.TokenOders.Where(o => o.User.Id == id).ToList();
            log.Info("Token Orders User : " + id);
            int AuctionNum = db.SystemParameters.FirstOrDefault().DefaultNumPageAuctions;
            int PageNum=1;
            if (page != null) PageNum = (int)page;
            return View(orders.ToPagedList(PageNum, AuctionNum));

        }
        public ActionResult ChangeParameters() {
            if (Session["Admin"] == null || Session["User"] != null || (bool)Session["Admin"]!=true)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            SystemParameter parameter = db.SystemParameters.FirstOrDefault();
            return View(parameter);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeParameters([Bind(Include = "DefaultNumPageAuctions,DefaultAuctionTime,SilverPackage,GoldPackage,PlatinumPackage,Currency,PriceOfToken")]SystemParameter param) {
            if (Session["Admin"] == null || Session["User"] != null || (bool)Session["Admin"] != true)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            param.Id = 1;
            db.Entry(param).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index", "Manage");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Bid(int idAuction, int tokensNum) {
            DateTime now = DateTime.UtcNow;
            if (Session["Admin"] != null || Session["User"] == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable)) {
                Auction a = db.Auctions.Find(idAuction);
                User u = db.Users.Find(((User)Session["User"]).Id);
                if (((DateTime)(a.OpenedOn)).AddSeconds((a.AuctionTime)) <= now) {
                    return RedirectToAction("SearchAuctions", "Manage");
                }

                double sum = tokensNum;
                Bid prev = null;
                if (a.Bids.Count > 0) 
                    prev= a.Bids.OrderByDescending(b => b.BidOn).First();
                //ako je neko bidovao pre mene:
                if (prev != null)
                {
                    if (tokensNum <= prev.Amount)
                    {
                        ViewBag.message = "Error: the amount od tokens is too small";
                        log.Info("Bid Fail User" + u.Id);
                        return RedirectToAction("SearchAuctions", "Manage");
                    }
                    if (prev.User.Id.Equals(u.Id))
                        sum = tokensNum - (double)prev.Amount;

                }
       
                if (tokensNum < a.StartPrice) {
                    ViewBag.message = "Error: the amount od tokens is too small";
                    log.Info("Bid Fail User" + u.Id);
                    return RedirectToAction("SearchAuctions", "Manage");
                }
                //ako ima prethodni bidder koji nisam ja vrati mu novac
                if (prev != null && !prev.User.Id.Equals(u.Id)) {
                    prev.User.TokensNumber += prev.Amount;
                }
                if (u.TokensNumber < sum) {
                    ViewBag.message = "You don't have anough Tokens!";
                    log.Info("Bid Fail User" + u.Id);
                    return RedirectToAction("SearchAuctions", "Manage");
                }
                u.TokensNumber -= sum;
                Bid bid = new Bid()
                {
                    Bidder = u.Id,
                    User = u,
                    Auction = idAuction,
                    Auction1 = db.Auctions.Find(idAuction),
                    BidOn = DateTime.UtcNow,
                    Amount = tokensNum,
                    Currency = db.SystemParameters.FirstOrDefault().Currency
                };
                if (ModelState.IsValid) {
                    ((User)Session["User"]).TokensNumber = bid.User.TokensNumber;
                    a.CurrentPrice = tokensNum;
                    a.CurUser = u.Id;
                    a.FullName = u.FirstName + " " + u.LastName;
                    db.Bids.Add(bid);
                    if(prev!=null) db.Entry(prev).State = EntityState.Modified;
                    db.Entry(u).State = EntityState.Modified;
                    await db.SaveChangesAsync();
                    transaction.Commit();
                    log.Info("Successful bid  by " + bid.User.Id + " on " + bid.Auction);
                    Hubs.AuctionHub.AuctionUpdate(idAuction, tokensNum, u.FirstName + " " + u.LastName);
                    ViewBag.message = "Your bid is accepted!";

                }
                return RedirectToAction("SearchAuctions", "Manage");

            }
        }



    }
}