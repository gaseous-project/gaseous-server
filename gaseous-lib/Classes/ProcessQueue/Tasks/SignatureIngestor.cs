using gaseous_server.Classes;

namespace gaseous_server.ProcessQueue.Plugins
{
    /// <summary>
    /// Represents a plugin for ingesting signature items.
    /// </summary>
    public class SignatureIngestor : QueueItemStatus, ITaskPlugin
    {
        /// <inheritdoc/>
        public QueueItemType ItemType => QueueItemType.SignatureIngestor;

        /// <inheritdoc/>
        public object? Data { get; set; }

        /// <inheritdoc/>
        public required QueueProcessor.QueueItem ParentQueueItem { get; set; }

        /// <inheritdoc/>
        public async Task Execute()
        {
            Logging.LogKey(Logging.LogType.Debug, "process.timered_event", "timered_event.starting_signature_ingestor");
            SignatureIngestors.XML.XMLIngestor tIngest = new SignatureIngestors.XML.XMLIngestor
            {
                CallingQueueItem = ParentQueueItem
            };

            // Set CallingQueueItem for status updates
            CallingQueueItem = ParentQueueItem;

            var parserTypes = Enum.GetValues(typeof(gaseous_signature_parser.parser.SignatureParser));
            int counter = 0;
            foreach (int i in parserTypes)
            {
                counter++;
                SetStatus(counter, parserTypes.Length, "Processing " + ((gaseous_signature_parser.parser.SignatureParser)i).ToString() + " signature files");
                gaseous_signature_parser.parser.SignatureParser parserType = (gaseous_signature_parser.parser.SignatureParser)i;
                if (
                    parserType != gaseous_signature_parser.parser.SignatureParser.Auto &&
                    parserType != gaseous_signature_parser.parser.SignatureParser.Unknown
                )
                {
                    Logging.LogKey(Logging.LogType.Debug, "process.signature_ingest", "signatureingest.processing_parser_files", null, new[] { parserType.ToString() });

                    string SignaturePath = Path.Combine(Config.LibraryConfiguration.LibrarySignaturesDirectory, parserType.ToString());
                    string SignatureProcessedPath = Path.Combine(Config.LibraryConfiguration.LibrarySignaturesProcessedDirectory, parserType.ToString());

                    if (!Directory.Exists(SignaturePath))
                    {
                        Directory.CreateDirectory(SignaturePath);
                    }

                    if (!Directory.Exists(SignatureProcessedPath))
                    {
                        Directory.CreateDirectory(SignatureProcessedPath);
                    }

                    tIngest.Import(SignaturePath, SignatureProcessedPath, parserType);
                }
            }
            ClearStatus();
        }
    }
}