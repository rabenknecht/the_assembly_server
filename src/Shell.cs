using System.Net;

namespace TheAssembly.Server;

public static class Shell
{
    private const string DEFAULT_CONFIG = "config.json";
    private const string COMMAND_NEWCONFIG = "newconfig";

    public static void Main(string[] args)
    {
        if ((args.Length == 1 || args.Length == 2) && args[0] == COMMAND_NEWCONFIG)
        {
            NewConfig(args.GetOr(1, DEFAULT_CONFIG));
        }
        else if ((args.Length == 1 || args.Length == 2) && args[0] == "run")
        {
            SetupAndRun(args.GetOr(1, DEFAULT_CONFIG));
        }
        else
        {
            PrintHelp();
        }
    }


    private static void PrintHelp()
    {
        Console.WriteLine("TODO: This should be a helptext. If you see this, hit Rabenknecht with a shovel so they actually write this shit");
    }


    private static void NewConfig(string newConfigFile)
    {
        try
        {
            File.WriteAllText(newConfigFile, Config.DefaultSerialized);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Exception while writing the configuration file ({newConfigFile}):\n{e}");
            return;
        }

        Console.WriteLine($"Successfully created default configuration file at \"{newConfigFile}\"");
    }


    private static void SetupAndRun(string configFile)
    {
        string rawConfigData;
        Config? config;

        try
        {
            rawConfigData = File.ReadAllText(configFile);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Exception while accessing the configuration file ({configFile}):\n{e}");
            return;
        }

        try
        {
            config = Config.Deserialize(rawConfigData);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Exception while parsing the content of the configuration file ({configFile}):\n{e}");
            return;
        }

        if (config == null)
        {
            Console.Error.WriteLine($"Parsing config file failed: Illegal json. Use \"{COMMAND_NEWCONFIG}\" to generate a default template for the config file");
            return;
        }

        if (File.Exists(config.DatabankPath))
        {
            Console.Error.WriteLine($"Databank path of config file is a file!");
            return;
        }

        Directory.CreateDirectory(config.DatabankPath);
        Console.WriteLine($"Successfully parsed configuration file at \"{configFile}\"");
        Run(config);
    }


    private static void Run(Config config)
    {
        var userStorage = new UserStorage(Path.Combine(config.DatabankPath, "users"));
        var server = new HttpListener();
        foreach (var prefix in config.ServerPrefixes)
        {
            // HttpListener does NOT like prefixes not ending with '/'
            if (!prefix.EndsWith('/')) server.Prefixes.Add(prefix + '/');
            else server.Prefixes.Add(prefix);
        }
        server.Start();
        Console.WriteLine($"Server online. Access me using: " + string.Join(", ", server.Prefixes));

        while (true)
        {
            // We will multithread at a later point
            var context = server.GetContext();
            var request = context.Request;
            var response = context.Response;
            var userId = ExtractUserId(request);
            var user = userId != null ? userStorage.Get(userId.Value) : null;
            // TODO: User Authentication

            var localPath = request.Url?.AbsolutePath[1..].ToLower().Split("/") ?? [];
            var requestMethod = request.HttpMethod.ToLower();

            long requestedId;
            byte[] rawData;
            User requestedUser;

            if (localPath == null)
            {
                response.StatusCode = (int) HttpStatusCode.NotFound;
            }
            // TODO: Check user authentication, and if user and requested user share at least one group
            else if (requestMethod == "get" && localPath.Length == 2
                && localPath[0] == "user" && long.TryParse(localPath[1], out requestedId)
                && userStorage.TryGetEncoded(requestedId, out rawData))
            {
                response.StatusCode = (int) HttpStatusCode.OK;
                response.OutputStream.Write(rawData);
            }
            else if (requestMethod == "post" && localPath.Length == 3
                && localPath[0] == "user" && localPath[2] == "default"
                && long.TryParse(localPath[1], out requestedId))
            {
                userStorage.Update(requestedId, new User(requestedId, "DefaultUser", []));
                response.StatusCode = (int) HttpStatusCode.OK;
            }
            else if (requestMethod == "post" && localPath.Length == 2
                && localPath[0] == "user" && long.TryParse(localPath[1], out requestedId)
                && User.TryDeserialize(request.InputStream, out requestedUser))
            {
                userStorage.Update(requestedId, requestedUser);
                response.StatusCode = (int) HttpStatusCode.OK;
            }
            else
            {
                response.StatusCode = (int) HttpStatusCode.NotFound;
            }

            response.Close();
        }
    }


    private static long? ExtractUserId(HttpListenerRequest request)
    {
        var cookie = request.Cookies.SingleOrDefault(c => c != null && c.Name == "userId");
        if (cookie == null) return null;
        if (long.TryParse(cookie.Value, out var userId)) return userId;
        return null;
    }
}
