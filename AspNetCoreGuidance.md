# ASP.NET Core Guidance

## Avoid using Stream.Read and Stream.Write, always use async overloads when reading from the request body or response body

## Use buffering and synchronous reads and writes as an alternative to asynchronous reading and writing

## Avoid reading the entire request body or response body into memory

## The HttpContext is NOT thread safe

## Do not use the HttpContext after the request is complete

## Prefer logging scopes with data over passing the HttpContext values into loggers directly

## Do not capture the HttpContext in background threads

## Do not capture services injected into the controllers on background threads

## Avoding writing to the response body after middleware pipeline executes
