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
            TypingEffect("Type 'exit' or 'quit' to end the chat.\n");

            // Map full user questions to specific topics
            var questionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    // Cybersecurity
    { "what is cybersecurity?", "cybersecurity" },
    { "why is cybersecurity important?", "cybersecurity" },
    { "how can i stay safe online?", "cybersecurity" },

    // Malware
    { "what is malware?", "malware" },
    { "what are different types of malware?", "malware" },
    { "how can i remove malware from my device?", "malware" },

    // Firewall
    { "what is a firewall?", "firewall" },
    { "how does a firewall protect my computer?", "firewall" },
    { "should i use a hardware or software firewall?", "firewall" },

    // Antivirus
    { "what does antivirus software do?", "antivirus" },
    { "is windows defender enough?", "antivirus" },
    { "how often should i run antivirus scans?", "antivirus" },

    // Network Security
    { "what are the basics of network security?", "network security" },
    { "how do i secure my home wi-fi?", "network security" },
    { "what is the difference between a public and private network?", "network security" },

    // Passwords
    { "what makes a strong password?", "password" },
    { "how often should i change my passwords?", "password" },
    { "are password managers safe to use?", "password" },

    // 2FA
    { "what is 2fa and how does it work?", "2fa" },
    { "is 2fa better than a strong password?", "2fa" },
    { "what are the best 2fa apps?", "2fa" },

    // Backup
    { "why should i back up my data?", "backup" },
    { "what are good backup strategies?", "backup" },
    { "should i use cloud or physical backups?", "backup" },

    // Safe Online Practices
    { "what are some safe browsing tips?", "safe online" },
    { "how can i tell if a website is secure?", "safe online" },
    { "what should i avoid sharing online?", "safe online" },

    // Phishing
    { "what is phishing?", "phishing" },
    { "how do i recognize phishing emails?", "phishing" },
    { "what should i do if i clicked a phishing link?", "phishing" },

    // VPN
    { "what is a vpn and how does it work?", "vpn" },
    { "do i really need a vpn?", "vpn" },
    { "are free vpns safe?", "vpn" },

    // Encryption
    { "what is encryption?", "encryption" },
    { "how is data encrypted?", "encryption" },
    { "how does encryption protect my privacy?", "encryption" },

    // Identity Theft
    { "what is identity theft?", "identity theft" },
    { "how do criminals steal identities?", "identity theft" },
    { "what should i do if my identity is stolen?", "identity theft" }
};


            // Define the main knowledge base: topics mapped to arrays of related answers
            var knowledgeBase = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
{
    // CYBERSECURITY
    { "cybersecurity", new[]
        {
            "Cybersecurity is the practice of protecting systems, networks, and programs from digital attacks.",
            "Cybersecurity is important because it helps prevent data breaches, identity theft, and disruptions to critical systems.",
            "To stay safe online, keep software updated, use strong passwords, enable 2FA, and avoid suspicious links."
        }
    },

    // MALWARE
    { "malware", new[]
        {
            "Malware is software designed to harm, exploit, or otherwise compromise a computer or network.",
            "Types of malware include viruses, worms, trojans, ransomware, spyware, and adware.",
            "To remove malware, use a trusted antivirus or anti-malware tool, run a full system scan, and follow removal instructions."
        }
    },

    // FIREWALL
    { "firewall", new[]
        {
            "A firewall is a security system that monitors and controls incoming and outgoing network traffic based on security rules.",
            "A firewall protects your computer by blocking unauthorized access while permitting outward communication.",
            "Both have value: hardware firewalls are better for networks, while software firewalls protect individual devices."
        }
    },

    // ANTIVIRUS
    { "antivirus", new[]
        {
            "Antivirus software detects, prevents, and removes malicious software from your devices.",
            "Windows Defender provides solid basic protection, but advanced users or businesses may need additional features.",
            "You should run antivirus scans at least once a week or set up real-time protection for continuous monitoring."
        }
    },

    // NETWORK SECURITY
    { "network security", new[]
        {
            "Network security involves protecting a network’s integrity, confidentiality, and accessibility from attacks.",
            "To secure your home Wi-Fi, change default credentials, use WPA3 encryption, and hide your SSID.",
            "Public networks are open and less secure, while private networks are secured with passwords and encryption."
        }
    },

    // PASSWORD
    { "password", new[]
        {
            "A strong password uses a mix of uppercase, lowercase, numbers, and symbols and avoids common words.",
            "It's recommended to change passwords every 3–6 months or immediately after a breach.",
            "Yes, password managers are generally safe and help generate and store complex passwords securely."
        }
    },

    // 2FA
    { "2fa", new[]
        {
            "2FA (Two-Factor Authentication) adds a second verification step—like a code sent to your phone—after entering your password.",
            "Yes, 2FA is more secure than a strong password alone, especially against phishing and brute-force attacks.",
            "Popular 2FA apps include Google Authenticator, Authy, and Microsoft Authenticator."
        }
    },

    // BACKUP
    { "backup", new[]
        {
            "Backing up data ensures you can recover files after device failure, malware, or accidental deletion.",
            "Good backup strategies include the 3-2-1 rule: 3 copies of data, on 2 types of storage, with 1 off-site.",
            "Cloud backups offer convenience and remote access, while physical backups provide fast recovery without internet."
        }
    },

    // SAFE ONLINE
    { "safe online", new[]
        {
            "Use updated browsers, block pop-ups, avoid suspicious links, and don’t download files from untrusted sites.",
            "A secure website uses HTTPS, shows a padlock icon in the address bar, and has a valid certificate.",
            "Avoid sharing personal info like full name, address, SSN, banking details, and login credentials online."
        }
    },

    // PHISHING
    { "phishing", new[]
        {
            "Phishing is a scam where attackers trick you into giving up personal info by pretending to be trustworthy.",
            "Signs of phishing emails include urgent language, suspicious links, unfamiliar senders, and grammar mistakes.",
            "If you clicked a phishing link, disconnect from the internet, run antivirus, and change any exposed passwords."
        }
    },

    // VPN
    { "vpn", new[]
        {
            "A VPN encrypts your internet traffic and routes it through a secure server, hiding your IP and location.",
            "Yes, a VPN enhances privacy and security, especially on public Wi-Fi or when accessing restricted content.",
            "Free VPNs can compromise privacy and limit speed—use a reputable paid provider for better safety."
        }
    },

    // ENCRYPTION
    { "encryption", new[]
        {
            "Encryption converts your data into unreadable code to protect it from unauthorized access.",
            "Data is encrypted using algorithms and keys, such as AES, to scramble information before transmission or storage.",
            "Encryption ensures that even if data is intercepted, it can't be read without the decryption key."
        }
    },

    // IDENTITY THEFT
    { "identity theft", new[]
        {
            "Identity theft is when someone uses your personal info—like SSN or bank details—without your permission.",
            "Criminals steal identities through phishing, data breaches, dumpster diving, or stealing physical documents.",
            "If your identity is stolen, report it to your bank, credit bureaus, and local authorities, and monitor your credit."
        }
    },
};
            // Suggest follow-up topics based on the current topic
            var followUps = new Dictionary<string, string[]>
            {
                { "malware", new[] { "antivirus", "backup" } },
                { "password", new[] { "2fa", "encryption" } },
                { "phishing", new[] { "identity theft", "safe online" } },
                { "vpn", new[] { "encryption", "network security" } },
            };

            // Map keywords in user input to relevant topics
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


            // Show main help topics
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

                // Exit condition
                if (userInput == "exit" || userInput == "quit")
                {
                    TypingEffect("Chatbot: Goodbye! Stay secure out there ;) ");
                    break;
                }

                // Show menu again
                if (userInput == "menu")
                {
                    DisplayMenu();
                    continue;
                }

                string matchedTopic = MatchTopic(userInput, knowledgeBase, keywordMap, questionMap);


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

        static string MatchTopic(string userInput, Dictionary<string, string[]> knowledgeBase, Dictionary<string, string> keywordMap, Dictionary<string, string> questionMap)
        {
            string normalized = userInput.Trim().ToLower();

            // 1. Exact match for known questions
            if (questionMap.ContainsKey(normalized))
            {
                return questionMap[normalized];
            }

            // 2. Keyword-based topic mapping
            foreach (var keyword in keywordMap)
            {
                if (normalized.Contains(keyword.Key))
                {
                    return keyword.Value;
                }
            }

            // 3. Topic name direct match
            foreach (var topic in knowledgeBase.Keys)
            {
                if (normalized.Contains(topic.ToLower()))
                {
                    return topic;
                }
            }

            return "cybersecurity"; // Default fallback
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
