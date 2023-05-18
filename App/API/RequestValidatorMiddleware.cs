//namespace Repository.App.API
//{
//    public class RequestValidatorMiddleware
//    {
//        private readonly RequestDelegate _next;

//        public RequestValidatorMiddleware(RequestDelegate next)
//        {
//            _next = next;
//        }

//        public async Task Invoke(HttpContext context)
//        {
//            //context.Request.EnableBuffering(100000000);
//            // Check if request body is empty and it's a multipart request
//            if ((context.Request.ContentLength == null || context.Request.ContentLength == 0)
//                && context.Request.ContentType != null
//                && context.Request.ContentType.ToUpper().StartsWith("MULTIPART/"))
//            {
//                Console.WriteLine("\n\nNo Multipart, bad request");
//                // Set 400 response with a message
//                context.Response.StatusCode = StatusCodes.Status400BadRequest;
//                await context.Response.WriteAsync("Multipart request body must not be empty.");
//            }
//            else
//            {
//                Console.WriteLine("\n\nHas content, good request.");
//                // All other requests continue the way down the pipeline
//                await _next(context);
//            }
//        }
//    }
//}
