﻿using jcIDS.web.DAL;
using jcIDS.web.Managers;
using jcIDS.web.Objects;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace jcIDS.web.Controllers
{
    public class AuthController : BaseApiController
    {
        public AuthController(IMemoryCache memoryCache, IDSContext dbContext, ConfigurationValues configuration) : base(memoryCache, dbContext, configuration)
        {
        }

        [HttpPost]
        public string Post(string deviceName)
        {
            if (!Configuration.AutoDeviceAdoption)
            {
                return string.Empty;
            }

            var device = new DeviceManager(Cache, DbContext).RegisterDevice(deviceName);

            return device.Token;

        }
    }
}