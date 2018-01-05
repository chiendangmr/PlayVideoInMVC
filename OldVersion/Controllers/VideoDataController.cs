using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PlayVideoInMVC.CustomDataResult;
using System.Web.Mvc;
using System.Configuration;
using PlayVideoInMVC.Models;
using System.Data.SqlClient;
using Dapper;
using HD.Web.Delay.Models.DAO;

namespace PlayVideoInMVC.Controllers
{
    public class VideoDataController : Controller
    {
        //
        // GET: /VideoData/
        public string _SubFolder = ConfigurationManager.AppSettings["SubFolder"];
        public string _CaptureLowresFolder = ConfigurationManager.AppSettings["CaptureLowresFolder"];
        public string _connectionString = ConfigurationManager.AppSettings["connString"];
        public string _logFolder = ConfigurationManager.AppSettings["LogFolder"];
        public string _Channel = ConfigurationManager.AppSettings["ChannelId"];
        public ActionResult Index()
        {
            return new VideoDataResult();
        }
        public ActionResult GetStartTime()
        {

            return Json(Convert.ToInt64((string)Session["StartTime"]), 0);
        }
        public ActionResult GetVideoToPlay(double Time)
        {
            var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
            var time = posixTime.AddMilliseconds(Time);
            var time_milisecon_tick = Time * 10000 + (new DateTime(1970, 1, 1)).Ticks;
            var time_milisecon = Time;
            List<RecordLowre> currentVideo = new List<RecordLowre>();
            using (HDDelay5Entities dc = new HDDelay5Entities())
            {
                currentVideo = dc.RecordLowres.Where(a => a.Deleted == false && a.Duration > 0).ToList();
            }
            string[] data = new string[4];
            foreach (RecordLowre rc in currentVideo)
            {
                if (time_milisecon > ToMiliSecond(rc.RecordTime) && time_milisecon < (ToMiliSecond(rc.RecordTime) + rc.Duration))
                {
                    data[0] = rc.FileName;
                    data[1] = ((int)((time_milisecon - ToMiliSecond(rc.RecordTime)) / 1000)).ToString();
                    data[2] = ToMiliSecond(rc.RecordTime).ToString();
                    data[3] = rc.Duration.ToString();
                }
            }
            return Json(data, 0);
        }
        public double ToMiliSecond(DateTime time)
        {
            return (time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)) + (new TimeSpan(7, 0, 0))).TotalMilliseconds;
        }
        public ActionResult GetVideoToPlay2(double Time)
        {
            var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
            var time = posixTime.AddMilliseconds(Time);
            var time_milisecon_tick = Time * 10000 + (new DateTime(1970, 1, 1)).Ticks;
            List<RecordLowre> currentVideo = new List<RecordLowre>();
            using (HDDelay5Entities dc = new HDDelay5Entities())
            {
                currentVideo = dc.RecordLowres.Where(a => a.Deleted == false && a.Duration > 0).ToList();
            }
            string[] data = new string[2];
            foreach (RecordLowre rc in currentVideo)
            {
                if (time_milisecon_tick > rc.RecordTime.Ticks && time_milisecon_tick < (rc.RecordTime.Ticks + rc.Duration * 10000))
                {
                    data[0] = rc.FileName;
                    data[1] = ((int)((time_milisecon_tick - rc.RecordTime.Ticks) / 10000000)).ToString();
                }
            }
            return Json(data, 0);
        }

        public ActionResult GetStatus()
        {
            var data = new bool[2];
            using (var db = new SqlConnection(_connectionString))
            {
                var result1 = db.Query<bool>(@" If Exists(Select ChannelId From CurrentItem Where ChannelId = @ChannelId and CurrentType = 0 and DATEADD(second, 10, LastTime) >= GETDATE())  Select convert(bit, 1)  Else Select convert(bit, 0)",
                    new
                    {
                        ChannelId = int.Parse(_Channel)
                    });

                var result2 = db.Query<bool>(@"If Exists(Select ChannelId From CurrentItem Where ChannelId = @ChannelId and CurrentType = 1 and DATEADD(second, 10, LastTime) >= GETDATE())   Select convert(bit, 1)  Else Select convert(bit, 0)",
                    new
                    {
                        ChannelId = int.Parse(_Channel)
                    });

                data[0] = result1.ToArray()[0];
                data[1] = result2.ToArray()[0];
            }
            return Json(data, 0);
        }
        public ActionResult GetStatusTimeRecordeAndPlay()
        {
            var data = new double[2];
            using (var db = new SqlConnection(_connectionString))
            {
                var result1 = db.Query<DateTime>(@"Select StartTime From CurrentItem Where ChannelId = @ChannelId and CurrentType = 0", new
                {
                    ChannelId = int.Parse(_Channel)
                });

                var result2 = db.Query<DateTime>(@"Select StartTime From CurrentItem Where ChannelId = @ChannelId and CurrentType = 1", new
                {
                    ChannelId = int.Parse(_Channel)
                });

                data[0] = result1.ToArray()[0].ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                data[1] = result2.ToArray()[0].ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            }
            return Json(data, 0);
        }

        public ActionResult GetSubtitleTime()
        {
            var data = new String[2];
            using (var db = new SqlConnection(_connectionString))
            {
                var result1 = db.Query<SubtitleTimeLine>(@"Select * From SubtitleTimeLine").Where(a => a.StartTime.Date == DateTime.Now.Date);
                int FileId = 0;
                string StartSubtitleTime = "";
                foreach (SubtitleTimeLine st in result1)
                {
                    if (st.StartTime.Day == DateTime.Today.Day)
                    {
                        FileId = st.FileId;
                        StartSubtitleTime = ToMiliSecond(st.StartTime).ToString();
                        // break;
                    }
                };
                var result2 = db.Query<SubtitleFileItem>(@"Select * From SubtitleFileItem where FileId = @File", new
                {
                    File = FileId
                }).ToList();
                string SubtitleDuration;
                SubtitleDuration = result2.Count > 0 ? result2[result2.Count - 1].StartTime.ToString() : " ";

                data[0] = StartSubtitleTime;
                data[1] = SubtitleDuration;
            }

            return Json(data, 0);
        }
    }
}
