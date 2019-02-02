using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using iep.Models;
using PagedList;
using System.Net;
using System.Data.Entity;
using System.IO;
using System.Data;
using log4net;
using System.Net.Mail;

namespace iep.Controllers
{
    public class ManageController : Controller {
        private dbcontext db = new dbcontext();
        private static log4net.ILog Log { get; set; }
        ILog log = log4net.LogManager.GetLogger(typeof(ManageController));

        public ActionResult Index() {
            return View();
        }

      
        public ActionResult SearchAuctions([Bind(Include = "Name, Status, HighPrice, LowPrice, Page")]AuctionViewModel avm) {
            var result = db.Auctions.OrderByDescending(a => a.CreatedOn).Where(x => x.Status!="READY").ToList();

            if (avm.Name!=null)
                result = result.Where(a => a.Name.Contains(avm.Name)).ToList();
            if(avm.HighPrice!=0)
                result = result.Where(a => a.CurrentPrice != null && a.StartPrice <= avm.HighPrice).ToList();
            if(avm.LowPrice!=0)
                result = result.Where(a => a.CurrentPrice != null && a.StartPrice >= avm.LowPrice).ToList();
            if (avm.Status!=null)
                result = result.Where(a => a.Status.Equals(avm.Status)).ToList();

            int AuctionNum = db.SystemParameters.FirstOrDefault().DefaultNumPageAuctions;
            int PageNum = 1;
            if (avm.Page != null) PageNum = (int)avm.Page;
            avm.AuctionList = result.ToPagedList(PageNum, AuctionNum);
            return View(avm);
        }

        public ActionResult CreateAuction() {
            if (Session["Admin"] != null || Session["User"] == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            return View();
        }

        private bool isOk(string name, string fullName, decimal? price){
            if (name == null || fullName == null || price == null || fullName.Length > 255 || name.Length>255)
                return false;
            else return true;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAuction([Bind(Include = "Name, StartPrice, AuctionTime, FullName")]Auction a, HttpPostedFileBase file) {
            if (Session["Admin"] != null || Session["User"] == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Auction auction = new Auction();
            var param = db.SystemParameters.First();

            auction.Currency = param.Currency;
            auction.Name = a.Name;
            if (a.AuctionTime > 0)
                auction.AuctionTime = a.AuctionTime;
            else
                auction.AuctionTime = param.DefaultAuctionTime;
            auction.CreatedOn = DateTime.UtcNow;
            auction.StartPrice = a.StartPrice;
            auction.CurrentPrice = auction.StartPrice;
            auction.FirstUser = ((User)(Session["User"])).Id;
            auction.FullName = a.FullName;
            auction.Status = "READY";

            if (file != null)
            {
                using (System.IO.MemoryStream ms = new MemoryStream())
                {
                    file.InputStream.CopyTo(ms);
                    auction.IMG = ms.GetBuffer();
                }
            }
            else return View();

            if (isOk(auction.Name, auction.FullName, auction.StartPrice))
            {
                db.Auctions.Add(auction);
                db.SaveChanges();
                return RedirectToAction("Index", "Manage");
            }
            else
                return View();
           
        }
        public ActionResult Details(int? Id) {
            if (Id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Auction a = db.Auctions.Find(Id);
            User owner = db.Users.Find(a.FirstUser);
            ViewBag.Owner = owner.FirstName + " " + owner.LastName;
            log.Info("Details Auction Name : " + a.Name);
            return View(a);
        }

        public ActionResult ReadyAuctions(int? page) {
            if (Session["User"] != null || Session["Admin"] == null || (bool)(Session["Admin"]) != true)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var auctions = db.Auctions.OrderByDescending(x => x.CreatedOn).Where(x => x.Status.Equals("READY")).ToList();
            int AuctionNum = db.SystemParameters.FirstOrDefault().DefaultNumPageAuctions;
            if (page == null) page = 1;
            int PageNum = (int)page;
            return View(auctions.ToPagedList(PageNum, AuctionNum));
        }

        public ActionResult OpenAuction(int id, int? ret) {
            if (Session["User"] != null || Session["Admin"] == null || (bool)(Session["Admin"]) != true)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Auction a = db.Auctions.Find(id);
            if(!a.Status.Equals("READY"))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            a.Status = "OPENED";
            a.OpenedOn = DateTime.UtcNow;
            db.Entry(a).State = EntityState.Modified;
           log.Info("Opened Auction Name : " + a.Name);
            db.SaveChanges();
            return RedirectToAction("ReadyAuctions", new { page = ret });
        }

        public ActionResult AuctionClose(int idAuction) {
            Auction a = db.Auctions.Find(idAuction);
            if (a != null) {
                DateTime time = ((DateTime)a.OpenedOn).AddSeconds(a.AuctionTime);
                //ako je sadasnje vreme posle vremena zatvaranje aukcije
                if (time <= DateTime.UtcNow) {
                    User owner = db.Users.Find(a.FirstUser);
                    owner.TokensNumber += (double?)a.CurrentPrice;
                    a.CompletedOn = DateTime.UtcNow;
                    a.Status = "COMPLETED";
                    db.Entry(a).State = EntityState.Modified;
                    db.Entry(owner).State = EntityState.Modified;
                    db.SaveChanges();
                    log.Info("Closed Auction Name : " + a.Name);
                }
            }
            return RedirectToAction("SearchAuctions", "Auction");
        }

        public ActionResult WonAuctions() {
            if (Session["Admin"] != null || Session["User"] == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            int id = ((User)(Session["User"])).Id;
            var auctions=db.Auctions.OrderByDescending(x => x.CreatedOn).Where(x => x.Status.Equals("COMPLETED") && x.CurUser==id).ToList();
            if (auctions.Count == 0) ViewBag.Message = "There are no won auctions!";
            return View(auctions);
        }

        public ActionResult OrderTokens()
        {
            return View();
        }

  
        public ActionResult OrderT(string package) {
            int amount=0;
            int id= ((User)(Session["User"])).Id;
            var par = db.SystemParameters.FirstOrDefault();
            if (package == "SilverPackage")
                amount = par.SilverPackage;
            if(package=="GoldPackage")
                amount = par.GoldPackage;
            if (package == "PlatinumPackage")
                amount = par.PlatinumPackage;
            TokenOder order=new TokenOder();

            if (amount!=0) {
                order.Buyer = ((User)(Session["User"])).Id;
                order.TokensAmount = amount;
                order.Price = par.PriceOfToken * amount;
                order.Currency = par.Currency;
                order.Status = "SUBMITTED";
                order.User = db.Users.Find(id);
                db.TokenOders.Add(order);
                db.SaveChanges();
                log.Info("Create Order-SUBMITTED User" + order.Buyer);
                return Redirect("http://stage.centili.com/payment/widget?apikey=125c01e3e293287b89aebb60254dd334&country=rs&reference=" + order.Id);
            }
            else return View("OrderTokens");
            
        }

        public ActionResult CentiliResponse(string clientid, string status) { 
        
            int id = Int32.Parse(clientid);
            TokenOder order = db.TokenOders.Find(id);
            if (order == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (status == "canceled" || status == "failed")
            {
                order.Status = "CANCELED";
                 log.Info("Create Order-CANCELED User" + order.Buyer);
            }
            else
            {
                order.Status = "COMPLETED";
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {

                        User u = db.Users.Find(order.Buyer);
                        if (u == null)
                            return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                        u.TokensNumber += order.TokensAmount;
                        ((User)Session["User"]).TokensNumber = u.TokensNumber;
                        db.Entry(u).State = EntityState.Modified;
                        db.SaveChanges();
                        transaction.Commit();
                        //sendMail(u.Email);
                        log.Info("Token order succeeded " + order.User.Email + "num " + order.TokensAmount);
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                        log.Info("Roll back token order failed " + order.User.Email + "num " + order.TokensAmount);
                        return RedirectToAction("SearchAuction", "Manage");
                    }
                }
            }
            db.Entry(order).State= EntityState.Modified;
            db.SaveChanges();
            return new HttpStatusCodeResult(200);
        }

        private void sendMail(string mail) {
            MailMessage mm = new MailMessage("iepprojekat2019@gmail.com", mail);
            {
                mm.Subject = "TokenOrder";
                string body = "Thank you for you order";
                mm.Body = body;
                mm.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.EnableSsl = true;
                NetworkCredential NetworkCred = new NetworkCredential("iepprojekat2019", "Jelena123!");
                smtp.UseDefaultCredentials = true;
                smtp.Credentials = NetworkCred;
                smtp.Port = 587;
                smtp.Send(mm);
            }
        }


    }  
}