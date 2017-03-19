﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CvarcWeb.Data;
using CvarcWeb.Models;
using CvarcWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CvarcWeb.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private const string adminEmail = "fokychuk47@ya.ru";
        private const string adminPass = "hardPasswd14";
        private static Random rand = new Random();
        private readonly IEmailSender emailSender;

        public AdminController(UserDbContext context, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            this.context = context;
            this.userManager = userManager;
            this.emailSender = emailSender;
        }

        public ActionResult Index()
        {
            if (!context.Users.Any(u => u.Email == adminEmail))
                userManager.CreateAsync(new ApplicationUser {Email = adminEmail, UserName = adminEmail}, adminPass)
                    .Wait();

            CheckAdminUser();

            return View();
        }

        [HttpPost]
        public ActionResult ChangePassword(string userName, string password)
        {
            CheckAdminUser();

            var user = context.Users.First(u => u.UserName == userName);
            var token = userManager.GeneratePasswordResetTokenAsync(user).Result;
            userManager.ResetPasswordAsync(user, token, password).Wait();
            return Content("OK");
        }

        [HttpPost]
        public ActionResult ITPlanetRegistrationFromCSV()
        {
            CheckAdminUser();

            var file = Request.Form.Files["RegInfo"];
            var ms = new MemoryStream();
            file.CopyTo(ms);
            var content = Encoding.UTF8.GetString(ms.ToArray());
            var lines = content.Split('\n');

            var messages = new List<string>();

            foreach (var line in lines)
            {
                var msg = "";
                try
                {
                    var data = line.Split(';');
                    msg += "splited! ";
                    var passwd = GeneratePass();
                    var user = new ApplicationUser
                    {
                        FIO = data[0],
                        Email = data[1],
                        UserName = data[1]
                    };
                    msg += "user parsed! ";
                    userManager.CreateAsync(user, passwd).Wait();
                    msg += "user registered! ";
                    var regedUser = context.Users.First(u => u.Email == data[1]);
                    var team = context.Teams.Add(new Team
                    {
                        CanOwnerLeave = false,
                        MaxSize = 1,
                        OwnerId = regedUser.Id,
                        CvarcTag = Guid.NewGuid()
                    });
                    context.SaveChanges();
                    regedUser.Team = team.Entity;
                    context.SaveChanges();
                    msg += "team registered! ";
                    emailSender.SendEmail(data[1], "IT-Planet credentials", $"Hi! This is your password: {passwd} \n here u can log in: homm.ulearn.me");
                    msg += "email sent";
                }
                catch (Exception e)
                {
                    msg += e.ToString();
                    messages.Add(msg);
                    break;
                }
                messages.Add(msg);
            }

            messages.Add($"registered {messages.Count}/{lines.Length}");

            return Content(string.Join("\n", messages));
        }

        private void CheckAdminUser()
        {
            if (User.Identity.Name != adminEmail)
                throw new Exception("fail");
        }

        private string GeneratePass()
        {
            var chars = "poqwieurytalsdkjfhmnbvcxzQWPERIOYWQWELKJSHADFGGMNXZBCZXMCNB82703456012";
            var pass = "";
            for (var i = 0; i < 8; i++)
                pass += chars[rand.Next(chars.Length - 1)];
            return pass;
        }
    }
}