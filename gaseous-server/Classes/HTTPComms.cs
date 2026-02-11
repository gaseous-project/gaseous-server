namespace gaseous_server.Classes
{
    /// <summary>
    /// Provides common high level HTTP communications methods such as GET, POST, PUT, DELETE, HEAD, etc. while handling errors, exceptions, logging, retries, timeouts, and other common concerns.
    /// Each method should accept parameters for expected return type using T, URL, headers, body (if applicable), timeout, and retry policy.
    /// Each method should return a standardized response object containing status code, headers, body, and any error information.
    /// It should also support asynchronous operations using async/await patterns.
    /// Where applicable, it should support JSON serialization/deserialization for request and response bodies.
    /// It should also include logging hooks to allow integration with logging frameworks for request/response logging.
    /// </summary>
    public class HTTPComms
    {
        private string _userAgent
        {
            get
            {
                // get the assembly version
                var assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                return $"GaseousServer/{assemblyVersion} (.NET {System.Environment.Version}; {System.Runtime.InteropServices.RuntimeInformation.OSDescription})";
            }
        }

        private HttpClient _httpClient = new HttpClient();

        private static int _defaultRetryCount = 3;

        private static TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

        // JsonSerializerOptions configured to handle property hiding/new keyword scenarios
        public static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = false,
            PreferredObjectCreationHandling = System.Text.Json.Serialization.JsonObjectCreationHandling.Populate,
            // This setting tells the serializer to prefer derived class properties over base class properties
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
            Converters = { new CaseInsensitiveEnumConverter(), new FlexibleNumberConverter() }
        };

        // Rate limiting parameters
        // time window in seconds
        private static int _rateLimitWindow = 60;
        // max requests allowed in the time window
        private static int _maxRequestsPerWindow = 100;
        private static int _rateLimit429WaitTimeSeconds = 60;
        private static int _rateLimit420WaitTimeSeconds = 120;

        // Track request timestamps for rate limiting per host
        private static readonly Dictionary<string, Queue<DateTime>> _hostRequestTimestamps = new Dictionary<string, Queue<DateTime>>();
        // Optional per-host rate limit overrides: domain -> options
        /// <summary>
        /// Options for per-host rate limiting configuration.
        /// </summary>
        public class RateLimitOptions
        {
            /// <summary>
            /// Time window in seconds for calculating the rate limit.
            /// </summary>
            public int WindowSeconds { get; set; }
            /// <summary>
            /// Maximum number of requests allowed within the time window.
            /// </summary>
            public int MaxRequests { get; set; }
        }
        private static readonly Dictionary<string, RateLimitOptions> _perHostRateLimits = new Dictionary<string, RateLimitOptions>();
        private static readonly object _rateLimitLock = new object();

        /// <summary>
        /// Sets rate limit options for a specific host.
        /// </summary>
        /// <param name="host">Domain name (e.g., example.com).</param>
        /// <param name="options">Rate limit options for the host.</param>
        public static void SetRateLimitForHost(string host, RateLimitOptions options)
        {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Host is required", nameof(host));
            if (options == null) throw new ArgumentNullException(nameof(options));
            lock (_rateLimitLock)
            {
                _perHostRateLimits[host] = options;
            }
        }

        /// <summary>
        /// Replaces all per-host rate limits with the provided dictionary.
        /// </summary>
        /// <param name="limits">Dictionary mapping host to rate limit options.</param>
        public static void SetRateLimits(Dictionary<string, RateLimitOptions> limits)
        {
            if (limits == null) throw new ArgumentNullException(nameof(limits));
            lock (_rateLimitLock)
            {
                _perHostRateLimits.Clear();
                foreach (var kvp in limits)
                {
                    _perHostRateLimits[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Represents the HTTP methods supported by HTTPComms.
        /// </summary>
        public enum HttpMethod
        {
            /// <summary>
            /// HTTP GET method.
            /// </summary>
            GET,
            /// <summary>
            /// HTTP POST method.
            /// </summary>
            POST,
            /// <summary>
            /// HTTP PUT method.
            /// </summary>
            PUT,
            /// <summary>
            /// HTTP DELETE method.
            /// </summary>
            DELETE,
            /// <summary>
            /// HTTP HEAD method.
            /// </summary>
            HEAD
        }

        /// <summary>
        /// Represents a standardized HTTP response containing status code, headers, body, and error information.
        /// </summary>
        /// <typeparam name="T">The type of the response body.</typeparam>
        public class HttpResponse<T>
        {
            /// <summary>
            /// Gets or sets the HTTP status code returned by the server.
            /// </summary>
            public int StatusCode { get; set; }

            /// <summary>
            /// Gets or sets the HTTP headers returned by the server.
            /// </summary>
            public Dictionary<string, string> Headers { get; set; }

            /// <summary>
            /// Gets or sets the body of the HTTP response.
            /// </summary>
            public T? Body { get; set; }

            /// <summary>
            /// Gets or sets the error message associated with the HTTP response, if any.
            /// </summary>
            public string? ErrorMessage { get; set; }

            /// <summary>
            /// Gets or sets the error type if an exception occurred.
            /// </summary>
            public string? ErrorType { get; set; }

            /// <summary>
            /// Gets or sets the error stack trace if an exception occurred.
            /// </summary>
            public string? ErrorStackTrace { get; set; }

            /// <summary>
            /// Optionally contains the raw HttpResponseMessage for advanced scenarios.
            /// </summary>
            public HttpResponseMessage? RawResponse { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="HttpResponse{T}"/> class.
            /// </summary>
            public HttpResponse()
            {
                Headers = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Creates a new <see cref="System.Text.Json.JsonSerializerOptions"/> instance with HTTPComms custom converters and settings.
        /// </summary>
        /// <returns>Configured <see cref="System.Text.Json.JsonSerializerOptions"/> with case-insensitive enums and flexible number handling.</returns>
        public static System.Text.Json.JsonSerializerOptions GetConfiguredOptions()
        {
            return new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                IncludeFields = false,
                PreferredObjectCreationHandling = System.Text.Json.Serialization.JsonObjectCreationHandling.Populate,
                TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
                Converters = { new CaseInsensitiveEnumConverter(), new FlexibleNumberConverter() }
            };
        }

        /// <summary>
        /// Sends an HTTP request asynchronously. Automatically enforces per-host rate limits, handles content types, supports cancellation and chunked downloads.
        /// </summary>
        /// <typeparam name="T">Expected response type. If <c>T</c> is <c>byte[]</c> or <c>string</c>, special handling is applied.</typeparam>
        /// <param name="method">HTTP method to use.</param>
        /// <param name="url">Request URL.</param>
        /// <param name="headers">Optional request headers.</param>
        /// <param name="body">Optional JSON-serializable body for POST/PUT.</param>
        /// <param name="timeout">Optional timeout; defaults to class default.</param>
        /// <param name="retryCount">Number of retries for transient failures (e.g., 429/420).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the request and any ongoing I/O operations.</param>
        /// <param name="returnRawResponse">If true, populates RawResponse in the returned object.</param>
        /// <param name="progressHandler">Optional progress callback for downloads: (bytesRead, totalBytes). Called on start, progress, completion.</param>
        /// <param name="chunkThresholdBytes">If content length exceeds this, stream in chunks; default 5 MB. Ignored unless T is byte[].</param>
        /// <param name="enableResume">When true and server supports ranges, attempts to resume downloads starting at <paramref name="resumeFromBytes"/>.</param>
        /// <param name="resumeFromBytes">Starting offset in bytes for resuming a download. Effective only when <paramref name="enableResume"/> is true and server supports ranges.</param>
        /// <param name="contentType">Optional content type for the request body (e.g., "application/json", "application/xml"). Defaults to "text/plain" if not specified.</param>
        /// <param name="jsonSerializerOptions">Optional custom JsonSerializerOptions for deserialization. If null, uses the default options configured for this class.</param>
        public async Task<HttpResponse<T>> SendRequestAsync<T>(HttpMethod method, Uri url, Dictionary<string, string>? headers = null, object? body = null, TimeSpan? timeout = null, int retryCount = 0, System.Threading.CancellationToken cancellationToken = default, bool returnRawResponse = false, Action<long, long?>? progressHandler = null, long chunkThresholdBytes = 5 * 1024 * 1024, bool enableResume = true, long resumeFromBytes = 0, string? contentType = null, System.Text.Json.JsonSerializerOptions? jsonSerializerOptions = null)
        {
            // Use provided options or fall back to default
            var options = jsonSerializerOptions ?? _jsonOptions;

            // Clear all previous headers from the HttpClient
            _httpClient.DefaultRequestHeaders.Clear();

            // Set User-Agent header
            _httpClient.DefaultRequestHeaders.Add("User-Agent", _userAgent);

            // Build a per-request timeout using a linked cancellation token (avoid mutating HttpClient.Timeout)
            using var timeoutCts = new System.Threading.CancellationTokenSource(timeout ?? _defaultTimeout);
            using var linkedCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            var effectiveToken = linkedCts.Token;

            // Add headers if provided
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            // Create empty response object
            var response = new HttpResponse<T>();

            int attempts = 0;
            int maxAttempts = retryCount > 0 ? retryCount : _defaultRetryCount;

            // Main retry loop
            while (attempts < maxAttempts)
            {
                // --- Rate limiting logic scoped to host ---
                // Extract host from URL
                var uri = url;
                string host = uri.Host;
                bool shouldWait = false;
                int waitMs = 0;
                lock (_rateLimitLock)
                {
                    DateTime now = DateTime.UtcNow;
                    // Get or create the queue for this host
                    if (!_hostRequestTimestamps.ContainsKey(host))
                    {
                        _hostRequestTimestamps[host] = new Queue<DateTime>();
                    }
                    var hostQueue = _hostRequestTimestamps[host];
                    // Rate limit window and max requests - use per-host override if available
                    int window = _rateLimitWindow;
                    int maxReq = _maxRequestsPerWindow;
                    if (_perHostRateLimits.TryGetValue(host, out var opts))
                    {
                        window = opts.WindowSeconds;
                        maxReq = opts.MaxRequests;
                    }
                    // Remove timestamps outside the window
                    while (hostQueue.Count > 0 && (now - hostQueue.Peek()).TotalSeconds > window)
                    {
                        hostQueue.Dequeue();
                    }
                    // If we've hit the max requests for the window, calculate how long to wait
                    if (hostQueue.Count >= maxReq)
                    {
                        shouldWait = true;
                        // Wait until the oldest request is outside the window
                        var oldest = hostQueue.Peek();
                        waitMs = (int)Math.Max(0, (window - (now - oldest).TotalSeconds) * 1000);
                    }
                }
                // If rate limit exceeded for this host, wait before sending the request
                if (shouldWait && waitMs > 0)
                {
                    await Task.Delay(waitMs, effectiveToken);
                }

                try
                {
                    HttpResponseMessage httpResponseMessage;

                    // Build the HttpRequestMessage
                    using (var requestMessage = new HttpRequestMessage(new System.Net.Http.HttpMethod(method.ToString()), url))
                    {
                        // Add body for POST/PUT requests
                        if (body != null && (method == HttpMethod.POST || method == HttpMethod.PUT))
                        {
                            string bodyContent;
                            if (body is string bodyString)
                            {
                                bodyContent = bodyString;
                            }
                            else
                            {
                                bodyContent = System.Text.Json.JsonSerializer.Serialize(body, options);
                            }

                            string effectiveContentType = contentType ?? "application/json";
                            requestMessage.Content = new StringContent(bodyContent, System.Text.Encoding.UTF8, effectiveContentType);
                        }

                        // If resuming a large binary download, add Range header
                        if (typeof(T) == typeof(byte[]) && enableResume && resumeFromBytes > 0 && (method == HttpMethod.GET))
                        {
                            requestMessage.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(resumeFromBytes, null);
                        }

                        // Send the request (respect cancellation)
                        httpResponseMessage = await _httpClient.SendAsync(requestMessage, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, effectiveToken);
                    }

                    // Record this request timestamp for rate limiting (per host)
                    lock (_rateLimitLock)
                    {
                        if (!_hostRequestTimestamps.ContainsKey(host))
                        {
                            _hostRequestTimestamps[host] = new Queue<DateTime>();
                        }
                        _hostRequestTimestamps[host].Enqueue(DateTime.UtcNow);
                    }

                    // Populate response object with status code and headers
                    response.StatusCode = (int)httpResponseMessage.StatusCode;
                    if (returnRawResponse)
                    {
                        response.RawResponse = httpResponseMessage;
                    }
                    foreach (var header in httpResponseMessage.Headers)
                    {
                        response.Headers[header.Key] = string.Join(", ", header.Value);
                    }
                    // Also include content headers
                    foreach (var header in httpResponseMessage.Content.Headers)
                    {
                        response.Headers[header.Key] = string.Join(", ", header.Value);
                    }

                    // Decide how to read based on content-type and T
                    var mediaType = httpResponseMessage.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
                    long? contentLength = httpResponseMessage.Content.Headers.ContentLength;
                    bool isBinary = mediaType != null && (mediaType.Contains("octet-stream") || mediaType.StartsWith("image/") || mediaType.StartsWith("video/") || mediaType.StartsWith("audio/"));

                    if (typeof(T) == typeof(byte[]))
                    {
                        // Determine if server supports resume via Accept-Ranges
                        bool serverSupportsRanges = response.Headers.TryGetValue("Accept-Ranges", out var acceptRangesVal) && acceptRangesVal.Contains("bytes");
                        long startingOffset = (enableResume && resumeFromBytes > 0 && serverSupportsRanges) ? resumeFromBytes : 0;

                        // Chunked download for large payloads, with progress
                        if ((contentLength.HasValue && contentLength.Value >= chunkThresholdBytes) || startingOffset > 0)
                        {
                            using var stream = await httpResponseMessage.Content.ReadAsStreamAsync(effectiveToken);
                            using var ms = new MemoryStream();
                            if (startingOffset > 0)
                            {
                                // Pre-size stream by writing zeros or set position; here we just track totalRead starting at startingOffset
                                ms.Position = 0; // we will append bytes; caller can decide how to persist resumed data externally
                            }
                            var buffer = new byte[81920];
                            long totalRead = 0;
                            // If resuming, initial progress starts at startingOffset
                            progressHandler?.Invoke(startingOffset, contentLength.HasValue ? contentLength + startingOffset : null);
                            int read;
                            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, effectiveToken)) > 0)
                            {
                                ms.Write(buffer, 0, read);
                                totalRead += read;
                                // Report progress including starting offset
                                var reportedTotal = startingOffset + totalRead;
                                var reportedLength = contentLength.HasValue ? contentLength + startingOffset : null;
                                progressHandler?.Invoke(reportedTotal, reportedLength);
                            }
                            progressHandler?.Invoke(startingOffset + totalRead, contentLength.HasValue ? contentLength + startingOffset : null);
                            response.Body = (T)(object)ms.ToArray();
                        }
                        else
                        {
                            var bytes = await httpResponseMessage.Content.ReadAsByteArrayAsync(effectiveToken);
                            response.Body = (T)(object)bytes;
                        }
                    }
                    else if (typeof(T) == typeof(string))
                    {
                        string responseBody = await httpResponseMessage.Content.ReadAsStringAsync(effectiveToken);
                        response.Body = (T)(object)responseBody;
                    }
                    else if (mediaType != null && (mediaType.Contains("json") || mediaType.StartsWith("text")))
                    {
                        string responseBody = await httpResponseMessage.Content.ReadAsStringAsync(effectiveToken);
                        if (!string.IsNullOrWhiteSpace(responseBody))
                        {
                            response.Body = System.Text.Json.JsonSerializer.Deserialize<T>(responseBody, options);
                        }
                    }
                    else if (mediaType != null && mediaType.Contains("xml"))
                    {
                        string responseBody = await httpResponseMessage.Content.ReadAsStringAsync(effectiveToken);
                        if (!string.IsNullOrWhiteSpace(responseBody))
                        {
                            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                            using var reader = new StringReader(responseBody);
                            response.Body = (T?)serializer.Deserialize(reader);
                        }
                    }
                    else if (isBinary)
                    {
                        var bytes = await httpResponseMessage.Content.ReadAsByteArrayAsync(effectiveToken);
                        // If T is not byte[], attempt to deserialize is risky; return default
                        if (typeof(T) == typeof(byte[]))
                            response.Body = (T)(object)bytes;
                    }
                    else
                    {
                        // Fallback: try JSON
                        string responseBody = await httpResponseMessage.Content.ReadAsStringAsync(effectiveToken);
                        if (!string.IsNullOrWhiteSpace(responseBody))
                        {
                            response.Body = System.Text.Json.JsonSerializer.Deserialize<T>(responseBody, options);
                        }
                    }

                    // Check for CloudFlare rate limiting first (error code: 1015 can come with 200 status)
                    bool isCloudFlareError = false;
                    if (response.Body != null && typeof(T) == typeof(byte[]))
                    {
                        try
                        {
                            string bodyText = System.Text.Encoding.UTF8.GetString((byte[])(object)response.Body!).ToLower();
                            if (bodyText.Contains("error code: 1015"))
                            {
                                isCloudFlareError = true;
                                attempts++;
                                if (attempts < maxAttempts)
                                {
                                    int waitTime = _rateLimit429WaitTimeSeconds;
                                    await Task.Delay(waitTime * 1000, effectiveToken);
                                    continue;
                                }
                            }
                        }
                        catch
                        {
                            // If unable to parse response body, continue with normal flow
                        }
                    }

                    // If request was successful and not a CloudFlare error, exit loop
                    if (httpResponseMessage.IsSuccessStatusCode && !isCloudFlareError)
                    {
                        break;
                    }

                    // If CloudFlare error with retries exhausted, break
                    if (isCloudFlareError && attempts >= maxAttempts)
                    {
                        break;
                    }

                    // Handle 429 Too Many Requests response
                    if (response.StatusCode == 429)
                    {
                        attempts++;
                        int waitTime = _rateLimit429WaitTimeSeconds;
                        // Check for Retry-After header and use it if present
                        if (response.Headers.ContainsKey("Retry-After"))
                        {
                            var retryVal = response.Headers["Retry-After"];
                            if (int.TryParse(retryVal, out int retryAfterSeconds))
                            {
                                waitTime = retryAfterSeconds;
                            }
                            else if (DateTime.TryParse(retryVal, out var retryDate))
                            {
                                var diff = (int)Math.Max(0, (retryDate.ToUniversalTime() - DateTime.UtcNow).TotalSeconds);
                                waitTime = diff;
                            }
                        }
                        // Wait before retrying
                        await Task.Delay(waitTime * 1000, effectiveToken);
                    }
                    // Handle 420 Enhance Your Calm response
                    else if (response.StatusCode == 420)
                    {
                        attempts++;
                        await Task.Delay(_rateLimit420WaitTimeSeconds * 1000, effectiveToken);
                    }
                    else if (!isCloudFlareError && !httpResponseMessage.IsSuccessStatusCode)
                    {
                        // For other errors, do not retry
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // On exception, record error and retry with exponential backoff
                    Logging.LogKey(Logging.LogType.Warning, "HTTPComms", $"Exception on attempt {attempts + 1} for {method} {url}: {ex.Message}", null, null, ex);
                    response.ErrorMessage = ex.Message;
                    response.ErrorType = ex.GetType().FullName;
                    response.ErrorStackTrace = ex.StackTrace;
                    attempts++;
                    await Task.Delay(2000 * attempts, effectiveToken);
                }
            }

            // Return the response object
            return response;
        }

        /// <summary>
        /// Checks download capabilities via a HEAD request: Accept-Ranges and Content-Length.
        /// </summary>
        /// <param name="url">The URL to inspect.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Tuple indicating AcceptRanges (bytes) and ContentLength if known.</returns>
        public async Task<(bool AcceptRanges, long? ContentLength)> CheckDownloadCapabilitiesAsync(Uri url, System.Threading.CancellationToken cancellationToken = default)
        {
            using var headRequest = new HttpRequestMessage(new System.Net.Http.HttpMethod("HEAD"), url);
            var headResponse = await _httpClient.SendAsync(headRequest, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            bool acceptRanges = false;
            long? contentLen = headResponse.Content.Headers.ContentLength;
            if (headResponse.Headers.TryGetValues("Accept-Ranges", out var ranges))
            {
                foreach (var v in ranges)
                {
                    if (v.Contains("bytes")) { acceptRanges = true; break; }
                }
            }
            return (acceptRanges, contentLen);
        }

        /// <summary>
        /// Downloads content to a file with optional resume support, progress reporting, and cancellation.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="destinationFilePath">The file path to write to.</param>
        /// <param name="headers">Optional headers to include with the request.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the download.</param>
        /// <param name="progressHandler">Optional progress callback: (bytesWritten, totalBytes).</param>
        /// <param name="overwrite">If true, deletes any existing file before downloading.</param>
        /// <returns>HTTP response containing any body bytes (also written to file) and status information.</returns>
        public async Task<HttpResponse<byte[]>> DownloadToFileAsync(Uri url, string destinationFilePath, Dictionary<string, string>? headers = null, System.Threading.CancellationToken cancellationToken = default, Action<long, long?>? progressHandler = null, bool overwrite = false)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrWhiteSpace(destinationFilePath)) throw new ArgumentException("Destination file path is required", nameof(destinationFilePath));

            // Overwrite handling
            if (overwrite && System.IO.File.Exists(destinationFilePath))
            {
                System.IO.File.Delete(destinationFilePath);
            }

            long existingLength = 0;
            if (System.IO.File.Exists(destinationFilePath))
            {
                var info = new System.IO.FileInfo(destinationFilePath);
                existingLength = info.Length;
            }

            // Capability pre-check
            var (acceptRanges, contentLen) = await CheckDownloadCapabilitiesAsync(url, cancellationToken);

            // Use SendRequestAsync with resume enabled; write bytes to file as they arrive
            var response = await SendRequestAsync<byte[]>(HttpMethod.GET, url, headers, null, TimeSpan.FromMinutes(5), _defaultRetryCount, cancellationToken, false, (read, total) =>
            {
                // Report cumulative progress including existingLength if resuming
                var reported = existingLength + read;
                long? totalWithExisting = total.HasValue ? existingLength + total.Value : (contentLen.HasValue ? existingLength + contentLen.Value : null);
                progressHandler?.Invoke(reported, totalWithExisting);
            },
            1 * 1024 * 1024, // stream when >1MB
            enableResume: acceptRanges,
            resumeFromBytes: existingLength);

            // Write file (overwrite or append depending on resume)
            if (response.Body != null && response.Body.Length > 0)
            {
                // If resuming, append; otherwise write new
                var mode = existingLength > 0 ? System.IO.FileMode.Append : System.IO.FileMode.Create;
                using var fs = new System.IO.FileStream(destinationFilePath, mode, System.IO.FileAccess.Write, System.IO.FileShare.None);
                await fs.WriteAsync(response.Body, 0, response.Body.Length, cancellationToken);
            }

            return response;
        }

        /// <summary>
        /// Custom JSON converter factory that handles case-insensitive enum parsing.
        /// </summary>
        private class CaseInsensitiveEnumConverter : System.Text.Json.Serialization.JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert)
            {
                return typeToConvert.IsEnum;
            }

            public override System.Text.Json.Serialization.JsonConverter? CreateConverter(Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
            {
                var converterType = typeof(CaseInsensitiveEnumConverterInner<>).MakeGenericType(typeToConvert);
                return (System.Text.Json.Serialization.JsonConverter?)Activator.CreateInstance(converterType);
            }

            private class CaseInsensitiveEnumConverterInner<T> : System.Text.Json.Serialization.JsonConverter<T> where T : struct, Enum
            {
                public override T Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
                {
                    switch (reader.TokenType)
                    {
                        case System.Text.Json.JsonTokenType.String:
                            var stringValue = reader.GetString();
                            if (stringValue != null)
                            {
                                foreach (var value in Enum.GetValues<T>())
                                {
                                    if (string.Equals(value.ToString(), stringValue, StringComparison.OrdinalIgnoreCase))
                                    {
                                        return value;
                                    }
                                }
                            }
                            // Log and skip unknown enum value, return default
                            Logging.LogKey(Logging.LogType.Warning, "CaseInsensitiveEnumConverter", $"Unknown enum value \"{stringValue}\" for enum \"{typeToConvert}\". Skipping and using default value.", null, null);
                            return default;

                        case System.Text.Json.JsonTokenType.Number:
                            if (reader.TryGetInt32(out int intValue))
                            {
                                return (T)Enum.ToObject(typeToConvert, intValue);
                            }
                            // Log and skip unknown numeric value
                            Logging.LogKey(Logging.LogType.Warning, "CaseInsensitiveEnumConverter", $"Unknown numeric enum value {intValue} for enum \"{typeToConvert}\". Skipping and using default value.", null, null);
                            return default;
                    }
                    Logging.LogKey(Logging.LogType.Warning, "CaseInsensitiveEnumConverter", $"Unexpected token {reader.TokenType} when parsing enum \"{typeToConvert}\". Using default value.", null, null);
                    return default;
                }

                public override void Write(System.Text.Json.Utf8JsonWriter writer, T value, System.Text.Json.JsonSerializerOptions options)
                {
                    writer.WriteStringValue(value.ToString());
                }

                public override T ReadAsPropertyName(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
                {
                    var stringValue = reader.GetString();
                    if (stringValue != null)
                    {
                        foreach (var value in Enum.GetValues<T>())
                        {
                            if (string.Equals(value.ToString(), stringValue, StringComparison.OrdinalIgnoreCase))
                            {
                                return value;
                            }
                        }
                    }
                    // Log and skip unknown enum value, return default
                    Logging.LogKey(Logging.LogType.Warning, "CaseInsensitiveEnumConverter", $"Unknown enum value \"{stringValue}\" for enum \"{typeToConvert}\" as property name. Skipping and using default value.", null, null);
                    return default;
                }

                public override void WriteAsPropertyName(System.Text.Json.Utf8JsonWriter writer, T value, System.Text.Json.JsonSerializerOptions options)
                {
                    writer.WritePropertyName(value.ToString());
                }
            }
        }

        private class FlexibleNumberConverter : System.Text.Json.Serialization.JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert)
            {
                return typeToConvert == typeof(int) || typeToConvert == typeof(int?) ||
                       typeToConvert == typeof(long) || typeToConvert == typeof(long?) ||
                       typeToConvert == typeof(uint) || typeToConvert == typeof(uint?) ||
                       typeToConvert == typeof(ulong) || typeToConvert == typeof(ulong?) ||
                       typeToConvert == typeof(short) || typeToConvert == typeof(short?) ||
                       typeToConvert == typeof(ushort) || typeToConvert == typeof(ushort?) ||
                       typeToConvert == typeof(byte) || typeToConvert == typeof(byte?) ||
                       typeToConvert == typeof(sbyte) || typeToConvert == typeof(sbyte?) ||
                       typeToConvert == typeof(float) || typeToConvert == typeof(float?) ||
                       typeToConvert == typeof(double) || typeToConvert == typeof(double?) ||
                       typeToConvert == typeof(decimal) || typeToConvert == typeof(decimal?);
            }

            public override System.Text.Json.Serialization.JsonConverter? CreateConverter(Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
            {
                var converterType = typeof(FlexibleNumberConverterInner<>).MakeGenericType(typeToConvert);
                return (System.Text.Json.Serialization.JsonConverter?)Activator.CreateInstance(converterType);
            }

            private class FlexibleNumberConverterInner<T> : System.Text.Json.Serialization.JsonConverter<T>
            {
                public override T? Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
                {
                    try
                    {
                        switch (reader.TokenType)
                        {
                            case System.Text.Json.JsonTokenType.Number:
                                return (T?)Convert.ChangeType(reader.GetDecimal(), Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert);
                            case System.Text.Json.JsonTokenType.String:
                                var stringValue = reader.GetString();
                                if (string.IsNullOrWhiteSpace(stringValue))
                                    return default;
                                return (T?)Convert.ChangeType(stringValue, Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert);
                            case System.Text.Json.JsonTokenType.Null:
                                return default;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.LogKey(Logging.LogType.Warning, "FlexibleNumberConverter", $"Failed to convert value to {typeToConvert.Name}: {ex.Message}", null, null);
                    }
                    return default;
                }

                public override void Write(System.Text.Json.Utf8JsonWriter writer, T? value, System.Text.Json.JsonSerializerOptions options)
                {
                    if (value == null)
                        writer.WriteNullValue();
                    else
                        writer.WriteRawValue(value.ToString() ?? "null");
                }
            }
        }
    }
}