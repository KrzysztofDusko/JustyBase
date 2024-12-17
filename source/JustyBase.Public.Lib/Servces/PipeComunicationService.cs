using System.Diagnostics;
using System.IO.Pipes;

namespace JustyBase.Public.Lib.Servces;

public sealed class PipeComunicationService(string jbMessagePipe)
{
    public Action<string>? ActivateOpenedFileAction { get; init; }
    public Action? RestoreAction { get; init; }
    public required Action<Exception> ExceptionAction { get; init; }

    private readonly string _jb_message_pipe = jbMessagePipe;

    public void Start()
    {
        Task waitForExternalMessagesTask = new(() => WaitForFileToOpenFromSystem(), TaskCreationOptions.LongRunning);
        waitForExternalMessagesTask.Start();
    }

    private void WaitForFileToOpenFromSystem()
    {
        while (true)
        {
            try
            {
                using NamedPipeServerStream pipeServer = new(_jb_message_pipe, PipeDirection.InOut);
                Debug.WriteLine("NamedPipeServerStream object created.");
                // Wait for a client to connect
                Debug.Write("Waiting for client connection...");
                pipeServer.WaitForConnection();
                Debug.WriteLine("Client connected.");

                try
                {
                    // Read user input and send that to the client process.
                    using StreamReader sr = new(pipeServer);
                    while (!sr.EndOfStream)
                    {
                        string? line = sr.ReadLine();
                        Debug.WriteLine(line);
                        if (File.Exists(line))
                        {
                            ActivateOpenedFileAction?.Invoke(line);
                        }
                        else if (line == "RESTORE")
                        {
                            RestoreAction?.Invoke();
                        }
                    }
                }
                // Catch the IOException that is raised if the pipe is broken
                // or disconnected.
                catch (IOException e)
                {
                    Debug.WriteLine("ERROR: {0}", e.Message);
                }
            }
            catch (Exception ex)
            {
                ExceptionAction?.Invoke(ex);
            }
        }
    }
}
