# Common Pitfalls writing scalable services in ASP.NET Core


## Avoid using Stream.Read and Stream.Write, always use async overloads when reading from the request body or response body.
