using PlayVideoInMVC.Controllers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;

namespace PlayVideoInMVC.CustomDataResult
{
	public class VideoDataResult : ActionResult
	{        
        public string VideoFolder = ConfigurationManager.AppSettings["VideoFolder"];

        /// <summary>
        /// The below method will respond with the Video file
        /// </summary>
        /// <param name="context"></param>
        /// 
        public override void ExecuteResult(ControllerContext context)
		{            
            var viewObj = new HomeController();
            string strVideoFilePath = "";
            strVideoFilePath = HostingEnvironment.MapPath(Path.Combine(VideoFolder, viewObj._VideoFile));
            context.HttpContext.Response.AddHeader("Content-Disposition", "attachment; filename=Recorded.mp4");

			var objFile = new FileInfo(strVideoFilePath);

			var stream = objFile.OpenRead();
			var objBytes = new byte[stream.Length];
			stream.Read(objBytes, 0, (int)objFile.Length);
			context.HttpContext.Response.BinaryWrite(objBytes);
		}               
    }
}