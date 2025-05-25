using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Media;
using System.Threading;
using System.Text.RegularExpressions;


namespace Cybersecurity_Chatbot
{
    class ConversationMemory
    {
        private Dictionary<string, TopicMemory> topicMemories = new Dictionary<string, TopicMemory>(StringComparer.OrdinalIgnoreCase);
        private int maxMemoryCount = 10;

        public void AddInteraction(string topic, string userInput, string botResponse)
        {
            if (!topicMemories.ContainsKey(topic))
            {
                if (topicMemories.Count >= maxMemoryCount)
                {
                    // Remove oldest topic memory
                    var oldestKey = topicMemories.Keys.First();
                    topicMemories.Remove(oldestKey);
                }
                topicMemories[topic] = new TopicMemory();
            }
            topicMemories[topic].AddInteraction(userInput, botResponse);
        }

        public bool HasDiscussed(string topic) => topicMemories.ContainsKey(topic);

        public string GetLastResponse(string topic)
            => topicMemories.ContainsKey(topic) ? topicMemories[topic].GetLastResponse() : null;

        public int GetTopicCount(string topic)
            => topicMemories.ContainsKey(topic) ? topicMemories[topic].InteractionCount : 0;
    }

    class TopicMemory
    {
        private List<(string userInput, string botResponse)> interactions = new List<(string, string)>();

        public void AddInteraction(string userInput, string botResponse)
        {
            interactions.Add((userInput, botResponse));
        }

        public string GetLastResponse()
            => interactions.Count > 0 ? interactions.Last().botResponse : null;

        public int InteractionCount => interactions.Count;
    }

    public class Program
    {
        static void Main(string[] args)
        {
            string lastTopic = null;
            Random rand = new Random();
            ConversationMemory memory = new ConversationMemory();

            Logo();

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("#############################################################");
            Console.WriteLine(" Welcome to the Cybersecurity Chatbot! Talk to me :)");
            Console.WriteLine("#############################################################");
            Console.ResetColor();

            try
            {
                string soundFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voiceMessage.wav");

                if (File.Exists(soundFilePath))
                {
                    using (SoundPlayer player = new SoundPlayer(soundFilePath))
                    {
                        player.PlaySync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Audio not played: " + ex.Message);
            }

            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("\n Please enter your name: ");
            Console.ResetColor();
            string userName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userName))
                userName = "User";

            TypingEffect($"\n Hello, {userName}! I'm here to help you with cybersecurity questions.");
            TypingEffect("You can ask anything about cybersecurity—no need to be formal!");
            TypingEffect("Not sure where to start? Just type 'menu' and pick a topic you’re curious about.\n");

            var knowledgeBase = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "cybersecurity", new[] {
                    "Cybersecurity is all about defending your devices and data from threats like hackers, viruses, and scams. It’s like locking your digital doors.",
                    "It covers things like secure passwords, network defense, malware protection, and more. Want to explore any of those?"
                }},
                { "network security", new[] {
                    "To protect your router: change the default password, enable WPA3, and update the firmware regularly.",
                    "Also, turn off remote management and disable WPS. These reduce common attack points on home networks."
                }},
                { "phishing", new[] {
                    "Phishing attacks try to trick you into giving away sensitive info—like fake emails that look real.",
                    "Always verify links, don't click suspicious attachments, and enable spam filters. Staying alert is your best defense."
                }},
                { "malware", new[] {
                    "Malware is software designed to harm your system. It includes viruses, spyware, ransomware, and more.",
                    "Use trusted antivirus tools, don’t download from sketchy sites, and keep your software updated."
                }},
                { "password", new[] {
                    "Strong passwords use 12+ characters, upper/lowercase letters, numbers, and symbols.",
                    "Avoid using names, birthdays, or common words. A password manager helps create and store complex ones."
                }},
                { "vpn", new[] {
                    "A VPN creates a private, secure tunnel for your internet traffic—great for public Wi-Fi.",
                    "Go with a no-log VPN provider, and enable it whenever you’re on a shared or open network."
                }},
                { "firewall", new[] {
                    "A firewall acts like a gatekeeper—it blocks unwanted or dangerous traffic from reaching your device.",
                    "Use both software firewalls on devices and hardware firewalls on routers for layered defense."
                }},
                { "2fa", new[] {
                    "Two-factor authentication (2FA) adds another layer beyond your password. Even if someone has your password, they can’t log in."
                }},
                { "encryption", new[] {
                    "Encryption turns your data into unreadable code unless someone has the right key.",
                    "Use encrypted messaging apps like Signal and encrypt your hard drive for sensitive files."
                }},
                { "antivirus", new[] {
                    "Antivirus software detects and removes harmful programs. Keep it updated and run full scans regularly.",
                    "Some tools also offer real-time monitoring to stop threats as they happen."
                }},
                { "backup", new[] {
                    "Backups save copies of your files in case something goes wrong—like ransomware or hardware failure.",
                    "Use cloud backups or external drives, and test your restore process regularly."
                }},
                { "safe online", new[] {
                    "Be cautious about what you click, only browse HTTPS sites, and limit personal info you share.",
                    "Keep software updated, use strong passwords, and avoid public Wi-Fi without protection."
                }},
                { "identity theft", new[] {
                    "Identity theft happens when someone steals your personal info to impersonate you.",
                    "Protect yourself by monitoring your credit, shredding documents, and using strong authentication."
                }},
            };

            var followUps = new Dictionary<string, string[]>
            {
                { "malware", new[] { "antivirus", "backup" } },
                { "password", new[] { "2fa", "encryption" } },
                { "phishing", new[] { "identity theft", "safe online" } },
                { "vpn", new[] { "encryption", "network security" } },
            };

            var keywordMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "router", "network security" },
                { "wifi", "network security" },
                { "secure router", "network security" },
                { "password", "password" },
                { "strong password", "password" },
                { "vpn", "vpn" },
                { "2fa", "2fa" },
                { "two factor", "2fa" },
                { "firewall", "firewall" },
                { "email scam", "phishing" },
                { "fake email", "phishing" },
                { "phishing", "phishing" },
                { "identity", "identity theft" },
                { "stolen identity", "identity theft" },
                { "virus", "malware" },
                { "malware", "malware" },
                { "antivirus", "antivirus" },
                { "backup", "backup" },
                { "safe online", "safe online" },
                { "encryption", "encryption" },
                { "data theft", "cybersecurity" },
                { "cyber attack", "cybersecurity" },
            };

            DisplayMenu();

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"\n{userName}: ");
                Console.ResetColor();
                string userInput = Console.ReadLine()?.ToLower();

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    TypingEffect("Chatbot: Can you please type a question?");
                    continue;
                }

                if (userInput == "exit" || userInput == "quit")
                {
                    TypingEffect("Chatbot: Goodbye! Stay secure out there ;) ");
                    break;
                }

                if (userInput == "menu")
                {
                    DisplayMenu();
                    continue;
                }

                string matchedTopic = MatchTopic(userInput, knowledgeBase, keywordMap);

                if (knowledgeBase.ContainsKey(matchedTopic))
                {
                    string[] responses = knowledgeBase[matchedTopic];
                    string lastResponse = memory.GetLastResponse(matchedTopic);
                    string randomResponse;

                    do
                    {
                        randomResponse = responses[rand.Next(responses.Length)];
                    } while (randomResponse == lastResponse && responses.Length > 1);

                    int topicCount = memory.GetTopicCount(matchedTopic);

                    if (lastTopic == matchedTopic)
                    {
                        TypingEffect($"Chatbot: Since you're still interested in {matchedTopic}, here's something else that might help:");
                    }
                    else if (topicCount > 1)
                    {
                        TypingEffect($"Chatbot: I see we've talked about {matchedTopic} before. Here's something new:");
                    }

                    TypingEffect($"Chatbot: {randomResponse}");

                    memory.AddInteraction(matchedTopic, userInput, randomResponse);
                    lastTopic = matchedTopic;

                    if (followUps.ContainsKey(matchedTopic))
                    {
                        string suggestion = followUps[matchedTopic][rand.Next(followUps[matchedTopic].Length)];
                        TypingEffect($"Chatbot: Curious about related topics? Try asking about {suggestion} next!");
                    }
                }
                else
                {
                    TypingEffect("Chatbot: Hmm, I didn’t catch that. Could you rephrase it or pick a topic from the menu?");
                }
            }
        }

        static string MatchTopic(string userInput, Dictionary<string, string[]> knowledgeBase, Dictionary<string, string> keywordMap)
        {
            foreach (var keyword in keywordMap)
            {
                if (userInput.Contains(keyword.Key))
                {
                    return keyword.Value;
                }
            }

            foreach (var topic in knowledgeBase.Keys)
            {
                if (userInput.Contains(topic.ToLower()))
                {
                    return topic;
                }
            }

            return "cybersecurity";
        }

        static void Logo()
        {
            Console.WriteLine(@"   ______          __                                                             _   _             ______            _    ");
            Console.WriteLine(@" .' ___  |        [  |                                                           (_) / |_          |_   _ \          / |_  ");
            Console.WriteLine(@"/ .'   \_|  _   __ | |.--.   .---.  _ .--.  .--.  .---.  .---.  __   _   _ .--.  __ `| |-' _   __    | |_) |   .--. `| |-' ");
            Console.WriteLine(@"| |        [ \ [  ]| '/'`\ \/ /__\\[ `/'`\]( (`\]/ /__\\/ /'`\][  | | | [ `/'`\][  | | |  [ \ [  ]   |  __'. / .'`\ \| |   ");
            Console.WriteLine(@"\ `.___.'\  \ '/ / |  \__/ || \__., | |     `'.'.| \__.,| \__.  | \_/ |, | |     | | | |,  \ '/ /   _| |__) || \__. || |,  ");
            Console.WriteLine(@" `.____ .'[\_:  / [__;.__.'  '.__.'[___]   [\__) )'.__.''.___.' '.__.'_/[___]   [___]\__/[\_:  /   |_______/  '.__.' \__/  ");
            Console.WriteLine(@"           \__.'                                                                          \__.'                            ");
            Console.WriteLine();
        }

        static void TypingEffect(string text)
        {
            foreach (char c in text)
            {
                Console.Write(c);
                Thread.Sleep(30);
            }
            Console.WriteLine();
        }

        static void DisplayMenu()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n==================== MENU: Cybersecurity Topics ====================");
            Console.WriteLine(" Ask me about:");
            Console.WriteLine(" - Cybersecurity     - Network Security     - Phishing");
            Console.WriteLine(" - Malware           - Passwords            - VPN");
            Console.WriteLine(" - Firewall          - 2FA                  - Encryption");
            Console.WriteLine(" - Antivirus         - Backup               - Identity Theft");
            Console.WriteLine(" - Safe Online Practices and more...");
            Console.WriteLine("====================================================================\n");
            Console.ResetColor();
        }
    }
}
