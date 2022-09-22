using CachingImplementationTest.Models;
using CachingImplementationTest.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CachingImplementationTest.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDistributedCache _distributedCache;

        private readonly List<Student> studentsList = new List<Student>()
        {
            new Student()
            {
                Id=1,
                Class="1st",
                Name = "ABC",
                RollNumber = 123
            },
            new Student()
            {
                Id=2,
                Class="2nd",
                Name = "DEF",
                RollNumber = 124
            },
            new Student()
            {
                Id=3,
                Class="3rd",
                Name = "GHI",
                RollNumber = 125
            },
            new Student()
            {
                Id=4,
                Class="4th",
                Name = "JKL",
                RollNumber = 126
            },
            new Student()
            {
                Id=5,
                Class="5th",
                Name = "MNO",
                RollNumber = 127
            }
        };

        public HomeController(ILogger<HomeController> logger, IDistributedCache distributedCache)
        {
            _logger = logger;
            _distributedCache = distributedCache;
        }

        public IActionResult Index()
        {
            return View();
        }

        //Using Distributed Caching technique
        public async Task<ActionResult> GetAllStudents()
        {
            const string cacheKey = "GetAllStudents";

            try
            {
                var studentsByteArr = await _distributedCache.GetAsync(cacheKey);

                var cachedStudentsList = studentsByteArr.FromByteArray<List<Student>>();

                if (cachedStudentsList != null && cachedStudentsList.Count > 0)
                {
                    return View(cachedStudentsList);
                }

                #region Setting the cache if not available in the cache
                byte[] studentsListToSetInByteArr = studentsList.ToByteArray();

                //Imitating the API call
                Thread.Sleep(5000);

                DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
                    SlidingExpiration = TimeSpan.FromSeconds(5)
                };

                await _distributedCache.SetAsync(cacheKey, studentsListToSetInByteArr, options);
                #endregion

                return View(studentsList);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Some exception occurred during Get All Students and Exception is {ex.Message} and Stack Trace is {ex.StackTrace}");
            }

            return View(new List<Student>());
        }

        //Setting Multiple different Data in cache asynchronously 
        public async Task<JsonResult> WriteDataToCacheAsync()
        {
            try
            {

                #region Creating different data to be cached as a Key-Value Pair
                Dictionary<string, string> allDataToBeCached = new Dictionary<string, string>();

                for (var i = 1; i <= 15; i++)
                {
                    allDataToBeCached.Add($"Key_{i}", $"KeyValue_{i}");
                }
                #endregion

                #region Setting Cache data asynchronously using batching and parallel tasks approach
                var batchSize = 10;
                int batchCount = Convert.ToInt32(allDataToBeCached.Keys.Count / batchSize) + 1;

                for (int i = 0; i < batchCount; i++)
                {
                    int recordsToBeSkipped = i * batchSize;

                    var singleBatchData = allDataToBeCached.Skip(recordsToBeSkipped).Take(batchSize);

                    DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
                        SlidingExpiration = TimeSpan.FromSeconds(20)
                    };

                    List<Task> tasksToExecute = new List<Task>();
                    foreach (var tsk in singleBatchData)
                    {
                        string cacheKey = tsk.Key;
                        byte[] dataToCache = tsk.Value.ToByteArray();

                        tasksToExecute.Add(_distributedCache.SetAsync(cacheKey, dataToCache, options));
                    }

                    await Task.WhenAll(tasksToExecute);
                }
                #endregion

                return Json(1);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Some exception occurred during Writing Data To Cache Async and Exception is {ex.Message} and Stack Trace is {ex.StackTrace}");
            }
            return Json(-1);
        }
    }
}
