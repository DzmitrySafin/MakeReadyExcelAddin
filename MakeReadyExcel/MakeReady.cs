using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MakeReadyGeneral.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using NLog;
using System.Globalization;
using System.Web;

namespace MakeReadyExcel
{
    internal class MakeReady
    {
        private const string BaseUrl = "https://www.makeready.by";
        private const string AccessCookieName = "markerA";
        private readonly ProductInfoHeaderValue ProductInfoHeader;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // <OPTION value="deab6b3283a4afa572e54406da3adbc4">09.02.2023 BY ЗачОтный стейдж.Февраль (Level 1)</OPTION>
        private static Regex competitionRegex = new Regex(@"<OPTION\s+value=""(?<id>\w+)"">(?<date>\d{2}\.\d{2}\.\d{4})\s+(?<country>\w{2})\s+(?<title>.+)\s+\(Level\s+(?<level>\d)\)</OPTION>", RegexOptions.IgnoreCase);
        // <FORM action="/cgi-bin/get.cgi?alias=userlevel" method=POST name="loginform_s" onsubmit="return checkform_s(this)">
        private static Regex loggedoutRegex = new Regex(@"<FORM\s+action=""/cgi-bin/get\.cgi\?alias=userlevel""\s+method=""?POST""?\s+name=""?loginform_s""?\s+onsubmit=""return\s+checkform_s\(this\)"">", RegexOptions.IgnoreCase);
        // <div class="FI">Сафин Дмитрий</div>
        private static Regex usernameRegex = new Regex(@"<div\s+class=""FI"">(?<name>[\w\s]+)</div>", RegexOptions.IgnoreCase);
        // <DIV class="auth_msg_red">[\N]Такого пользователя нет в базе данных. <P>Попробуйте авторизоваться снова.</P>[\N]</DIV>
        private static Regex loginfailRegex = new Regex(@"<DIV\s+class=""auth_msg_red"">\n(?<msg1>\w+)<P>(?<msg2>\w+)</P>\n</DIV>", RegexOptions.IgnoreCase);
        // <TR><TD NOWRAP class='tip' tt='Dva 10 paper(s), 0 popper(s), 0 plate(s), 0 disappear, 5 penalty'>Stage 2</TD><TD NOWRAP align=right id='stagemaxpts_2'>100</TD><TD NOWRAP align=right>15%</TD><TD align=right>32.69</TD><TD NOWRAP align=right>38.01</TD><TD><small>C=0.8,&nbsp;D=1.5,&nbsp;PT=3.8,&nbsp;M=5.7</small></TD></TR>
        private static Regex stagesRegex = new Regex(@"<TR><TD\s+NOWRAP\s+class='tip'\s+tt='(?<name>[\w\s\d\(!/,\-\.\)]*?)\s+(?<paper>\d+)\s+paper\(s\),\s+(?<popper>\d+)\s+popper\(s\),\s+(?<plate>\d+)\s+plate\(s\),\s+(?<disappear>\d+)\s+disappear,\s+(?<penalty>\d+)\s+penalty'>Stage\s+(?<number>\d+)</TD><TD\s+NOWRAP\s+align=right\s+id='stagemaxpts_\d+'>(?<points>\d+)</TD>.+?</TR>", RegexOptions.IgnoreCase);

        #region Login settings

        public string LoginToken { get; set; }
        public string LoginEmail { get; set; }
        public string UserName { get; set; }
        private DateTime _loginTimestamp { get; set; }
        public DateTime LoginTimestamp
        {
            get { return _loginTimestamp; }
            set
            {
                // MakeReadyExcel.Properties.Settings.Default DateTime value might throw NullReferenceException for unknown reason
                // so, had to initialize it with dummy value '1/1/1900'
                _loginTimestamp = value.Year >= 2000 ? value : DateTime.MinValue;
            }
        }

        #endregion

        public bool IsLoggedIn => !string.IsNullOrEmpty(LoginToken);

        public string ResponseContent { get; private set; }

        public MakeReady()
        {
            var asm = Assembly.GetCallingAssembly();
            ProductInfoHeader = new ProductInfoHeaderValue(asm.GetCustomAttribute<AssemblyProductAttribute>().Product, asm.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
        }

        public async Task<bool?> Login(string email, string password)
        {
            var loginData = new Dictionary<string, string>()
            {
                ["login"] = email,
                ["password"] = password,
                ["twlauth"] = "1",
                ["alias"] = "userlevel",
                ["68726764862620f82de7df4c35410719"] = "1",
                ["nocache"] = "yes",
                ["url_or_www"] = "0.1887",
                ["twlspecmsefld"] = "aqu7a"
            };

            var cookies = new CookieContainer();
            var handler = new HttpClientHandler();
            handler.CookieContainer = cookies;

            using (var client = new HttpClient(handler, true))
            {
                client.BaseAddress = new Uri(BaseUrl);

                cookies.Add(client.BaseAddress, new Cookie(AccessCookieName, ""));
                using (var request = new HttpRequestMessage(HttpMethod.Post, "/cgi-bin/get.cgi?alias=userlevel"))
                {
                    request.Headers.UserAgent.Add(ProductInfoHeader);

                    using (var content = new FormUrlEncodedContent(loginData))
                    {
                        request.Content = content;
                        try
                        {
                            var response = await client.SendAsync(request);
                            if (!response.IsSuccessStatusCode) throw new Exception($"Could not get proper response from MakeReady (StatusCode: {response.StatusCode}, {response.ReasonPhrase})");
                            ResponseContent = await response.Content.ReadAsStringAsync();
                            logger.Debug($"[HTML PAGE CONTENT]\n{ResponseContent}\n[END OF HTML PAGE CONTENT]");
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                            return null;
                        }
                    }
                }

                LoginToken = "";
                var responseCookies = cookies.GetCookies(client.BaseAddress).Cast<Cookie>();
                foreach (Cookie cookie in responseCookies)
                {
                    if (cookie.Name == AccessCookieName)
                    {
                        LoginToken = cookie.Value;
                        break;
                    }
                }

                if (IsLoggedIn)
                {
                    Match match = usernameRegex.Match(ResponseContent);
                    if (match.Success) UserName = match.Groups["name"].Value;
                    LoginTimestamp = DateTime.Now;
                    LoginEmail = email;
                }
                else
                {
                    Match match = loginfailRegex.Match(ResponseContent);
                    if (match.Success)
                    {
                        logger.Warn("Received 'Login failed' response.");
                    }
                    else
                    {
                        logger.Warn("Could not log in. Response not recognized.");
                    }
                }

                return IsLoggedIn;
            }
        }

        public async Task<bool?> Logout()
        {
            var cookies = new CookieContainer();
            var handler = new HttpClientHandler();
            handler.CookieContainer = cookies;

            using (var client = new HttpClient(handler, true))
            {
                client.BaseAddress = new Uri(BaseUrl);

                cookies.Add(client.BaseAddress, new Cookie(AccessCookieName, LoginToken));
                cookies.Add(client.BaseAddress, new Cookie("login", ""));
                cookies.Add(client.BaseAddress, new Cookie("logout", ""));
                cookies.Add(client.BaseAddress, new Cookie("password", ""));
                using (var request = new HttpRequestMessage(HttpMethod.Get, "/cgi-bin/CMS/logout.cgi"))
                {
                    //request.Headers.UserAgent.Add(ProductInfoHeader);
                    request.Headers.Add("User-Agent", "Mozilla/5.0 AppleWebKit/537.36 Chrome/114.0.0.0");

                    try
                    {
                        var response = await client.SendAsync(request);
                        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Found) throw new Exception($"Could not get proper response from MakeReady (StatusCode: {response.StatusCode}, {response.ReasonPhrase})");
                        ResponseContent = await response.Content.ReadAsStringAsync();
                        logger.Debug($"[HTML PAGE CONTENT]\n{ResponseContent}\n[END OF HTML PAGE CONTENT]");
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        return null;
                    }
                }
            }

            if (string.IsNullOrEmpty(ResponseContent) || loggedoutRegex.IsMatch(ResponseContent))
            {
                UserName = "";
                LoginToken = "";
                return true;
            }
            else
            {
                logger.Warn("Could not log out. Response not recognized.");
                return false;
            }
        }

        public async Task<List<Competition>> LoadCompetitions(Func<Task<bool>> loginCallback = null)
        {
            var cookies = new CookieContainer();
            var handler = new HttpClientHandler();
            handler.CookieContainer = cookies;

            using (var client = new HttpClient(handler, true))
            {
                client.BaseAddress = new Uri(BaseUrl);

                cookies.Add(client.BaseAddress, new Cookie(AccessCookieName, LoginToken));
                using (var request = new HttpRequestMessage(HttpMethod.Get, "/performance"))
                {
                    request.Headers.UserAgent.Add(ProductInfoHeader);

                    try
                    {
                        var response = await client.SendAsync(request);
                        if (!response.IsSuccessStatusCode) throw new Exception($"Could not get proper response from MakeReady (StatusCode: {response.StatusCode}, {response.ReasonPhrase})");
                        ResponseContent = await response.Content.ReadAsStringAsync();
                        logger.Debug($"[HTML PAGE CONTENT]\n{ResponseContent}\n[END OF HTML PAGE CONTENT]");
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        return null;
                    }

                    var matches = competitionRegex.Matches(ResponseContent);
                    if (matches.Count == 0)
                    {
                        if (loggedoutRegex.IsMatch(ResponseContent) && loginCallback != null)
                        {
                            logger.Warn("Received 'Logged out' response. Trying to log in before loading list of matches...");
                            if (await loginCallback.Invoke())
                            {
                                return await LoadCompetitions();
                            }
                        }
                    }
                    if (matches.Count == 0)
                    {
                        logger.Warn("Could not load matches. Response not recognized.");
                    }

                    var competitions = new List<Competition>();
                    foreach (Match match in matches)
                    {
                        string date = match.Groups["date"].Value;
                        DateTime.TryParseExact(date, "dd.MM.yyyy", null, DateTimeStyles.None, out DateTime dt);
                        int.TryParse(match.Groups["level"].Value, out int level);
                        var competition = new Competition(match.Groups["id"].Value, dt, level, match.Groups["country"].Value, match.Groups["title"].Value);
                        //competition.Country = Countries.FirstOrDefault(c => c.Code == competition.CountryCode);
                        competitions.Add(competition);
                    }

                    return competitions;
                }
            }
        }

        public async Task<Tuple<List<Shooter>, List<Stage>>> LoadShooters(string competitionId, Func<Task<bool>> loginCallback = null)
        {
            var cookies = new CookieContainer();
            var handler = new HttpClientHandler();
            handler.CookieContainer = cookies;

            using (var client = new HttpClient(handler, true))
            {
                client.BaseAddress = new Uri(BaseUrl);

                cookies.Add(client.BaseAddress, new Cookie(AccessCookieName, LoginToken));
                using (var request = new HttpRequestMessage(HttpMethod.Get, $"/json_matchshooters?m={competitionId}"))
                {
                    request.Headers.UserAgent.Add(ProductInfoHeader);

                    try
                    {
                        var response = await client.SendAsync(request);
                        if (!response.IsSuccessStatusCode) throw new Exception($"Could not get proper response from MakeReady (StatusCode: {response.StatusCode}, {response.ReasonPhrase})");
                        ResponseContent = await response.Content.ReadAsStringAsync();
                        logger.Debug($"[HTML PAGE CONTENT]\n{ResponseContent}\n[END OF HTML PAGE CONTENT]");
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        return null;
                    }

                    if (loggedoutRegex.IsMatch(ResponseContent) && loginCallback != null)
                    {
                        logger.Warn("Received 'Logged out' response. Trying to log in before loading list of shooters...");
                        if (await loginCallback.Invoke())
                        {
                            return await LoadShooters(competitionId);
                        }
                    }

                    try
                    {
                        var shooters = JsonConvert.DeserializeObject<List<Shooter>>(ResponseContent);
                        if (shooters == null || shooters.Count == 0) throw new Exception($"Could not load shooters for match {competitionId}");
                        List<Stage> stages = null;
                        foreach (var shooter in shooters.Where(s => !s.Name.StartsWith("MegaBeast")).ToList())
                        {
                            stages = await LoadStages(competitionId, shooter.Id);
                            if (stages != null && stages.Count > 0) break;
                        }
                        if (stages == null || stages.Count == 0) throw new Exception($"Could not load stages for match {competitionId}");
                        return new Tuple<List<Shooter>, List<Stage>>(shooters, stages);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        return null;
                    }
                }
            }
        }

        private async Task<List<Stage>> LoadStages(string competitionId, int shooterId)
        {
            var cookies = new CookieContainer();
            var handler = new HttpClientHandler();
            handler.CookieContainer = cookies;

            using (var client = new HttpClient(handler, true))
            {
                client.BaseAddress = new Uri(BaseUrl);
                string urlKey = $"/json_shooteraccuracy?m={competitionId}&s={shooterId}";

                cookies.Add(client.BaseAddress, new Cookie(AccessCookieName, LoginToken));
                using (var request = new HttpRequestMessage(HttpMethod.Get, urlKey))
                {
                    request.Headers.UserAgent.Add(ProductInfoHeader);

                    try
                    {
                        var response = await client.SendAsync(request);
                        if (response.IsSuccessStatusCode)
                        {
                            ResponseContent = await response.Content.ReadAsStringAsync();
                            logger.Debug($"[HTML PAGE CONTENT]\n{ResponseContent}\n[END OF HTML PAGE CONTENT]");
                        }
                        else
                        {
                            if (response.StatusCode == HttpStatusCode.NotModified)
                            {
                                logger.Warn($"Request limit reached for match {competitionId}");
                            }
                            throw new Exception($"Could not get proper response from MakeReady (StatusCode: {response.StatusCode}, {response.ReasonPhrase})");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        return null;
                    }

                    try
                    {
                        var jToken = JArray.Parse(ResponseContent)[6]["tabledata"];
                        string tableData = jToken.ToString();
                        var matches = stagesRegex.Matches(tableData);
                        if (matches.Count == 0) throw new Exception("Could not parse stages data from MakeReady response");

                        var stages = new List<Stage>();
                        foreach (Match match in matches)
                        {
                            stages.Add(new Stage
                            {
                                StageNumber = int.Parse(match.Groups["number"].Value),
                                StageName = match.Groups["name"].Value,
                                PaperNumber = int.Parse(match.Groups["paper"].Value),
                                PopperNumber = int.Parse(match.Groups["popper"].Value),
                                PlateNumber = int.Parse(match.Groups["plate"].Value),
                                DisappearNumber = int.Parse(match.Groups["disappear"].Value),
                                PenaltyNumber = int.Parse(match.Groups["penalty"].Value),
                                MaxPoints = int.Parse(match.Groups["points"].Value)
                            });
                        }
                        return stages.OrderBy(s => s.StageNumber).ToList();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        return null;
                    }
                }
            }
        }

        public async Task<List<StageResult>> LoadAccuracy(string competitionId, int shooterId, Func<Task<bool>> loginCallback = null)
        {
            var cookies = new CookieContainer();
            var handler = new HttpClientHandler();
            handler.CookieContainer = cookies;

            using (var client = new HttpClient(handler, true))
            {
                client.BaseAddress = new Uri(BaseUrl);
                string urlKey = $"/json_shooteraccuracy?m={competitionId}&s={shooterId}";

                cookies.Add(client.BaseAddress, new Cookie(AccessCookieName, LoginToken));
                using (var request = new HttpRequestMessage(HttpMethod.Get, urlKey))
                {
                    request.Headers.UserAgent.Add(ProductInfoHeader);

                    try
                    {
                        var response = await client.SendAsync(request);
                        if (response.IsSuccessStatusCode)
                        {
                            ResponseContent = await response.Content.ReadAsStringAsync();
                            logger.Debug($"[HTML PAGE CONTENT]\n{ResponseContent}\n[END OF HTML PAGE CONTENT]");
                        }
                        else
                        {
                            if (response.StatusCode == HttpStatusCode.NotModified)
                            {
                                logger.Warn($"Request limit reached for match {competitionId}");
                            }
                            throw new Exception($"Could not get proper response from MakeReady (StatusCode: {response.StatusCode}, {response.ReasonPhrase})");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        return null;
                    }

                    if (loggedoutRegex.IsMatch(ResponseContent) && loginCallback != null)
                    {
                        logger.Warn("Received 'Logged out' response. Trying to log in before loading shooter's results...");
                        if (await loginCallback.Invoke())
                        {
                            return await LoadAccuracy(competitionId, shooterId);
                        }
                    }

                    try
                    {
                        var jToken = JArray.Parse(ResponseContent)[6]["tabledata"];
                        string tableData = jToken.ToString();
                        var regex = new Regex($@"<TR\s+id='{shooterId}'><TD\s+NOWRAP\s+class='tip'\s+tt='(?<name>[\w\s\d\(!/,\-\.\)]*?)\s+(?<paper>\d+)\s+paper\(s\),\s+(?<popper>\d+)\s+popper\(s\),\s+(?<plate>\d+)\s+plate\(s\),\s+(?<disappear>\d+)\s+disappear,\s+(?<penalty>\d+)\s+penalty'>Stage\s+(?<number>\d+)</TD><TD.*?>(?<alpha>\d+)</TD><TD.*?>(?<charlie>\d+)</TD><TD.*?>(?<delta>\d+)</TD><TD.*?>(?<miss>\d+)</TD><TD.*?>(?<noshoot>\d+)</TD><TD.*?>(?<penalty>\d+)</TD><TD.*?>.*?</TD><TD.*?>\d*</TD><TD.*?>(?<time>[\d\.]+)</TD>.+?</TR>", RegexOptions.IgnoreCase);
                        var matches = regex.Matches(tableData);
                        if (matches.Count == 0) throw new Exception("Could not parse shooter data from MakeReady response");

                        var accuracy = new List<StageResult>();
                        foreach (Match match in matches)
                        {
                            accuracy.Add(new StageResult
                            {
                                StageNumber = int.Parse(match.Groups["number"].Value),
                                AlphaCount = int.Parse(match.Groups["alpha"].Value),
                                CharlieCount = int.Parse(match.Groups["charlie"].Value),
                                DeltaCount = int.Parse(match.Groups["delta"].Value),
                                MissCount = int.Parse(match.Groups["miss"].Value),
                                NoshootCount = int.Parse(match.Groups["noshoot"].Value),
                                PenaltyCount = int.Parse(match.Groups["penalty"].Value),
                                TimeTaken = double.Parse(match.Groups["time"].Value, NumberStyles.Float, CultureInfo.InvariantCulture)
                            });
                        }
                        return accuracy.OrderBy(ac => ac.StageNumber).ToList();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        return null;
                    }
                }
            }
        }
    }
}
