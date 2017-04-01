﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Globalization;
namespace cabshare
{
    public class DBquery
    {
        public static async Task<cleandata> Clean(LUIS data)
        {
            cleandata clean = new cleandata();
            try
            {
                clean.origin = data.entities.Where(b => b.type == "location::fromlocation").FirstOrDefault().entity;
            }
            catch {
                clean.origin = "";
            }
            try
            {
                clean.dest = data.entities.Where(b => b.type == "location::tolocation").FirstOrDefault().entity;

            }
            catch
            {
                clean.dest = "";
            }
            try
            {
                string str = data.entities.Where(b => (b.type == "builtin.datetime.date")).FirstOrDefault().resolution.date.Replace("XXXX", "2017");
                clean.date = Convert.ToDateTime(str);

            }
            catch
            {
                clean.date = null;
            }
            try
            {
                if (DateTime.TryParseExact(data.entities.Where(b => (b.type == "builtin.datetime.time")).FirstOrDefault().resolution.time.Substring(1), "HH", CultureInfo.InvariantCulture, DateTimeStyles.None, out clean.time)) { }
                else if (DateTime.TryParseExact(data.entities.Where(b => (b.type == "builtin.datetime.time")).FirstOrDefault().resolution.time.Substring(1), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out clean.time)) { }
                else if (DateTime.TryParseExact(data.entities.Where(b => (b.type == "builtin.datetime.time")).FirstOrDefault().resolution.time.Substring(1), "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out clean.time)) { }
                //clean.time = DateTime.ParseExact(data.entities.Where(b => (b.type == "builtin.datetime.time")).FirstOrDefault().resolution.time.Substring(1), "HH", CultureInfo.InvariantCulture);
            }
            catch
            {
                return clean;
            }
            return clean;
        }
        public static async Task<List<Request>> dataquery(cleandata data)
        {

            using (var DB = new travelrecordEntities())
            {
                var match = (from b in DB.Requests where (b.origin == data.origin || data.origin == "") && (b.destination == data.dest || data.dest == "") && (b.date1 == data.date || data.date == null) && (b.time1 == data.time.TimeOfDay || data.time == default(DateTime)) select b).ToList();
                return match;

            }
        }
        public static async Task<List<Request>> showdata(string username)
        {
            using (var DB = new travelrecordEntities())
            {
                var match = (from b in DB.Requests where (b.name == username) select b).ToList();
                return match;
            }
        }
        public static async Task<string> addquery(cleandata data, string username,string psid,string fbid)
        {
            using (var DB = new travelrecordEntities())
            {
                Request user = new Request();
                user.id = DB.Requests.Max(p => p.id) + 1;
                user.name = username;
                user.origin = data.origin;
                user.destination = data.dest;
                user.date1 = data.date;
                user.time1 = data.time.TimeOfDay;
                user.fbid = int.Parse(fbid);
                user.psid = int.Parse(psid);
                user.MAXNO = data.noop;
                DB.Requests.Add(user);
                await DB.SaveChangesAsync();
            }
            return "Request added";
        }
    }
}