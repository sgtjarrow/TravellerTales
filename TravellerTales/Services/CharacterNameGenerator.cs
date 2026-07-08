using TravellerTales.Models;

namespace TravellerTales.Services;

public static class CharacterNameGenerator
{
    private const int MaximumDuplicateRetries = 20;

    private static readonly string[] HumanSuffixes = ["II", "III", "IV", "Jr", "Sr", "Esq"];
    private static readonly string[] CommonMaleFirstNames =
    [
        "Alex", "Adrian", "Caleb", "Darius", "Elias", "Felix", "Jonas", "Kai", "Leon", "Marcus",
        "Nolan", "Orion", "Petra", "Rami", "Theo", "Victor", "Wesley", "Zane"
    ];
    private static readonly string[] CommonFemaleFirstNames =
    [
        "Amara", "Carmen", "Elena", "Iris", "Leah", "Mara", "Nadia", "Selene", "Talia", "Vera",
        "Ari", "Diana", "Eva", "Lina", "Mira", "Naomi", "Rhea", "Sonia", "Yara", "Zara"
    ];
    private static readonly string[] CommonLastNames =
    [
        "Aster", "Cross", "Drake", "Hale", "Keller", "Mercer", "Nova", "Reyes", "Vale", "Ward",
        "Arden", "Bishop", "Caine", "Dane", "Ellis", "Frost", "Gray", "Maddox", "Stone", "West"
    ];
    private static readonly Dictionary<HeritageType, string[]> HeritageMaleFirstNames = new()
    {
        [HeritageType.Anglo] = ["Arthur", "Edwin", "Henry", "James", "Oliver", "William", "Alfred", "Benedict", "Charles", "Edward", "Frederick", "George"],
        [HeritageType.Arabic] = ["Amir", "Farid", "Hadi", "Samir", "Tariq", "Zayn", "Adil", "Bassam", "Hakim", "Idris", "Khalil", "Omar"],
        [HeritageType.Bantu] = ["Amani", "Jabari", "Kito", "Mosi", "Taye", "Zuberi", "Baraka", "Chuma", "Jengo", "Kwame", "Omari", "Sekou"],
        [HeritageType.Celtic] = ["Aiden", "Bran", "Cian", "Declan", "Finn", "Rowan", "Alastair", "Callum", "Eamon", "Gareth", "Lachlan", "Owen"],
        [HeritageType.Chinese] = ["An", "Jian", "Liang", "Shen", "Wei", "Yun", "Bo", "Chen", "Guo", "Jun", "Lei", "Tao"],
        [HeritageType.French] = ["Etienne", "Luc", "Marcel", "Remy", "Sebastien", "Thierry", "Alain", "Bastien", "Claude", "Gaston", "Henri", "Julien"],
        [HeritageType.Germanic] = ["Ansel", "Emil", "Lukas", "Otto", "Rolf", "Stefan", "Bruno", "Dieter", "Franz", "Gustav", "Hugo", "Klaus"],
        [HeritageType.Greek] = ["Damon", "Nikos", "Petros", "Stavros", "Theron", "Yannis", "Alexios", "Cosmas", "Dorian", "Helios", "Leander", "Orion"],
        [HeritageType.Hispanic] = ["Diego", "Elio", "Javier", "Mateo", "Rafael", "Santiago", "Alejandro", "Carlos", "Emilio", "Hector", "Luis", "Tomas"],
        [HeritageType.Indian] = ["Arjun", "Dev", "Kiran", "Ravi", "Rohan", "Vikram", "Amit", "Ishan", "Nikhil", "Pranav", "Sanjay", "Vivek"],
        [HeritageType.Italian] = ["Dante", "Gianni", "Luca", "Marco", "Nico", "Sandro", "Alberto", "Enzo", "Fabio", "Leonardo", "Matteo", "Vito"],
        [HeritageType.Japanese] = ["Daichi", "Hiro", "Kenji", "Ren", "Sora", "Yuki", "Akio", "Haruto", "Kaito", "Masaru", "Riku", "Takeshi"],
        [HeritageType.Korean] = ["Dae", "Hyun", "Jin", "Min", "Seo", "Tae", "Chul", "Do", "Hwan", "Joon", "Kyung", "Won"],
        [HeritageType.NativeAmerican] = ["Chayton", "Enapay", "Mika", "Nodin", "Takoda", "Tyee", "Ahanu", "Bodaway", "Hania", "Kele", "Misu", "Tahoma"],
        [HeritageType.Nordic] = ["Bjorn", "Erik", "Leif", "Magnus", "Soren", "Torsten", "Anders", "Einar", "Gunnar", "Harald", "Ivar", "Stellan"],
        [HeritageType.Persian] = ["Arman", "Cyrus", "Farhad", "Rostam", "Saman", "Yasin", "Bahram", "Darius", "Kian", "Navid", "Omid", "Peyman"],
        [HeritageType.Polynesian] = ["Akamu", "Koa", "Makoa", "Tane", "Tavita", "Toa", "Afa", "Ikaika", "Keanu", "Mana", "Pono", "Tui"],
        [HeritageType.Slavic] = ["Dimitri", "Ivan", "Marek", "Nikolai", "Sasha", "Viktor", "Boris", "Davor", "Luka", "Milan", "Pavel", "Yuri"],
        [HeritageType.SoutheastAsian] = ["Binh", "Dara", "Kiet", "Sokha", "Vinh", "Wirat", "Anurak", "Chai", "Huy", "Kham", "Somchai", "Vannak"],
        [HeritageType.WestAfrican] = ["Adama", "Ayo", "Kofi", "Tunde", "Yaro", "Zane", "Babatunde", "Chidi", "Dayo", "Femi", "Kwesi", "Osei"]
    };
    private static readonly Dictionary<HeritageType, string[]> HeritageFemaleFirstNames = new()
    {
        [HeritageType.Anglo] = ["Clara", "Eleanor", "Grace", "Rose", "Victoria", "Willa", "Alice", "Beatrice", "Charlotte", "Edith", "Florence", "Matilda"],
        [HeritageType.Arabic] = ["Dalia", "Layla", "Mariam", "Noor", "Samira", "Zahra", "Amina", "Farah", "Hana", "Jamila", "Nadia", "Rania"],
        [HeritageType.Bantu] = ["Asha", "Imani", "Lulu", "Nia", "Zola", "Zuri", "Amara", "Eshe", "Kamaria", "Nala", "Sanaa", "Zahra"],
        [HeritageType.Celtic] = ["Carys", "Eira", "Fiona", "Maeve", "Nessa", "Rhiannon", "Aisling", "Brigid", "Deirdre", "Enya", "Isla", "Siobhan"],
        [HeritageType.Chinese] = ["Jia", "Lan", "Mei", "Qiao", "Xiu", "Yun", "Ai", "Fang", "Hui", "Lian", "Ning", "Xia"],
        [HeritageType.French] = ["Camille", "Elise", "Mireille", "Noelle", "Sabine", "Yvette", "Amelie", "Celeste", "Colette", "Isabelle", "Lucienne", "Sophie"],
        [HeritageType.Germanic] = ["Elsa", "Greta", "Klara", "Lena", "Tilda", "Ursula", "Adela", "Brigitte", "Frida", "Heidi", "Ingrid", "Marta"],
        [HeritageType.Greek] = ["Ariadne", "Calla", "Elena", "Ione", "Petra", "Thalia", "Calista", "Daphne", "Helena", "Kore", "Lydia", "Phoebe"],
        [HeritageType.Hispanic] = ["Alma", "Ines", "Lucia", "Marisol", "Paloma", "Sofia", "Camila", "Elena", "Isabel", "Luna", "Marina", "Valeria"],
        [HeritageType.Indian] = ["Anika", "Isha", "Mira", "Priya", "Sana", "Veda", "Asha", "Deepa", "Kavya", "Leela", "Nisha", "Tara"],
        [HeritageType.Italian] = ["Alessia", "Bianca", "Gianna", "Lucia", "Siena", "Valeria", "Adriana", "Chiara", "Elisa", "Francesca", "Grazia", "Rosa"],
        [HeritageType.Japanese] = ["Aiko", "Hana", "Mika", "Naomi", "Sakura", "Yumi", "Akari", "Emi", "Haruka", "Keiko", "Rina", "Yuna"],
        [HeritageType.Korean] = ["Ara", "Hana", "Ji", "Min", "Sora", "Yuna", "Bora", "Eun", "Hye", "Jisoo", "Mina", "Seoyeon"],
        [HeritageType.NativeAmerican] = ["Aponi", "Kiona", "Nita", "Tala", "Winona", "Yoki", "Aiyana", "Halona", "Istas", "Kimama", "Nizhoni", "Sakari"],
        [HeritageType.Nordic] = ["Astrid", "Freya", "Ingrid", "Liv", "Saga", "Tove", "Anja", "Birgit", "Elin", "Kari", "Linnea", "Signe"],
        [HeritageType.Persian] = ["Darya", "Leila", "Roya", "Soraya", "Yasmin", "Ziba", "Anahita", "Azar", "Farah", "Laleh", "Nasrin", "Parisa"],
        [HeritageType.Polynesian] = ["Ailani", "Lani", "Noelani", "Pele", "Talia", "Waiola", "Alamea", "Halia", "Kalani", "Leilani", "Moana", "Nalani"],
        [HeritageType.Slavic] = ["Anya", "Irina", "Katya", "Mila", "Nadia", "Sasha", "Alina", "Danica", "Elena", "Lada", "Mira", "Zora"],
        [HeritageType.SoutheastAsian] = ["Anong", "Mai", "Mali", "Nari", "Sokha", "Thao", "Chantha", "Dao", "Kanya", "Linh", "Sophea", "Trang"],
        [HeritageType.WestAfrican] = ["Amina", "Ayo", "Mara", "Sade", "Yara", "Zola", "Abena", "Ama", "Esi", "Kadi", "Nkechi", "Yewande"]
    };
    private static readonly Dictionary<HeritageType, string[]> HeritageLastNames = new()
    {
        [HeritageType.Anglo] = ["Bennett", "Carter", "Hawthorne", "Sinclair", "Wells"],
        [HeritageType.Arabic] = ["Haddad", "Malik", "Nasser", "Rahman", "Sayegh"],
        [HeritageType.Bantu] = ["Biko", "Kabila", "Mbeki", "Ndlovu", "Okoro"],
        [HeritageType.Celtic] = ["Callahan", "MacRae", "Orr", "Quinn", "Sloane"],
        [HeritageType.Chinese] = ["Chen", "Huang", "Lin", "Wang", "Zhao"],
        [HeritageType.French] = ["Beaumont", "Laurent", "Moreau", "Rousseau", "Vidal"],
        [HeritageType.Germanic] = ["Adler", "Bauer", "Klein", "Richter", "Weiss"],
        [HeritageType.Greek] = ["Alexis", "Dukas", "Karras", "Nicolis", "Stavros"],
        [HeritageType.Hispanic] = ["Alvarez", "Castillo", "Morales", "Navarro", "Santos"],
        [HeritageType.Indian] = ["Kapoor", "Mehta", "Nair", "Rao", "Sen"],
        [HeritageType.Italian] = ["Conti", "Ferraro", "Marino", "Ricci", "Vella"],
        [HeritageType.Japanese] = ["Arai", "Kato", "Mori", "Sato", "Tanaka"],
        [HeritageType.Korean] = ["Choi", "Han", "Kim", "Park", "Seo"],
        [HeritageType.NativeAmerican] = ["Begay", "Chee", "Honanie", "Mankiller", "Yazzie"],
        [HeritageType.Nordic] = ["Berg", "Dahl", "Hansen", "Lund", "Nygaard"],
        [HeritageType.Persian] = ["Azar", "Farzan", "Mehr", "Navid", "Ramin"],
        [HeritageType.Polynesian] = ["Akamu", "Kealoha", "Mahoe", "Tupou", "Vaitai"],
        [HeritageType.Slavic] = ["Kovalenko", "Novak", "Petrov", "Sokolov", "Volkov"],
        [HeritageType.SoutheastAsian] = ["Dao", "Nguyen", "Phan", "Sok", "Tran"],
        [HeritageType.WestAfrican] = ["Adeyemi", "Diallo", "Mensah", "Okafor", "Traore"]
    };
    private static readonly Dictionary<HeritageType, NamePhonetics> HeritagePhonetics = new()
    {
        [HeritageType.Anglo] = new(["Al", "Ed", "Har", "Wil", "Ro", "Be"], ["win", "ton", "ley", "ward", "ford", "son"]),
        [HeritageType.Arabic] = new(["A", "Fa", "Ha", "Ka", "Sa", "Za"], ["mir", "din", "rah", "id", "im", "ir"]),
        [HeritageType.Bantu] = new(["A", "Ja", "Ko", "Mo", "Nu", "Za"], ["mani", "bari", "tayo", "lani", "kito", "zuri"]),
        [HeritageType.Celtic] = new(["Ai", "Bra", "Ca", "De", "Fi", "Ro"], ["den", "lan", "rys", "wan", "na", "gan"]),
        [HeritageType.Chinese] = new(["An", "Bo", "Ji", "Li", "Mei", "Yun"], ["an", "ang", "ei", "en", "ing", "uo"]),
        [HeritageType.French] = new(["Ba", "Cam", "El", "Lu", "Mar", "Re"], ["elle", "ien", "ette", "ard", "ette", "on"]),
        [HeritageType.Germanic] = new(["An", "Brun", "Els", "Ger", "Kla", "Ste"], ["sel", "hard", "win", "bert", "ric", "a"]),
        [HeritageType.Greek] = new(["Ale", "Da", "Io", "Ni", "Pe", "The"], ["xios", "mon", "na", "kos", "ra", "ron"]),
        [HeritageType.Hispanic] = new(["Al", "Ca", "Die", "Lu", "Mar", "So"], ["ma", "los", "go", "cia", "isol", "fia"]),
        [HeritageType.Indian] = new(["An", "De", "Ki", "Mi", "Ra", "Vi"], ["ika", "v", "ran", "ra", "vi", "kram"]),
        [HeritageType.Italian] = new(["Al", "Dan", "Gi", "Lu", "Mar", "Va"], ["ia", "te", "anna", "ca", "co", "eria"]),
        [HeritageType.Japanese] = new(["Ai", "Ha", "Ken", "Mi", "Ren", "Yu"], ["ko", "na", "ji", "ka", "to", "ki"]),
        [HeritageType.Korean] = new(["Ara", "Dae", "Ha", "Jin", "Min", "Seo"], ["ra", "hyun", "na", "woo", "ji", "yeon"]),
        [HeritageType.NativeAmerican] = new(["Apo", "Cha", "Kio", "No", "Ta", "Win"], ["ni", "ton", "na", "din", "la", "ona"]),
        [HeritageType.Nordic] = new(["As", "Bjo", "Ei", "Fre", "Lei", "So"], ["trid", "rn", "nar", "ya", "f", "ren"]),
        [HeritageType.Persian] = new(["Ar", "Dar", "Far", "Lei", "Sor", "Yas"], ["man", "ya", "had", "la", "aya", "min"]),
        [HeritageType.Polynesian] = new(["Ai", "Ka", "La", "Ma", "Noe", "Ta"], ["lani", "oa", "ni", "koa", "lani", "ne"]),
        [HeritageType.Slavic] = new(["An", "Di", "Ir", "Mi", "Ni", "Za"], ["ya", "mitri", "ina", "la", "kolai", "ra"]),
        [HeritageType.SoutheastAsian] = new(["An", "Bi", "Da", "Mai", "So", "Vi"], ["ong", "nh", "ra", "li", "kha", "nh"]),
        [HeritageType.WestAfrican] = new(["A", "Ko", "Ma", "Sa", "Tun", "Ye"], ["dama", "fi", "ra", "de", "de", "mi"])
    };
    private static readonly string[] AslanFamilyStarts = ["Akh", "Arr", "Gha", "Khar", "Mrr", "Rao", "Sah", "Trr", "Urr", "Zha"];
    private static readonly string[] AslanFamilyEnds = ["akha", "arru", "eirr", "ghao", "khir", "mara", "raou", "sakh", "tari", "zurr"];
    private static readonly string[] AslanPersonalStarts = ["A", "Ekh", "Farr", "Hra", "Kha", "Mia", "Nyr", "Rra", "Sia", "Tao"];
    private static readonly string[] AslanPersonalEnds = ["ash", "eir", "ha", "khai", "mir", "rra", "sao", "tah", "ya", "zir"];
    private static readonly string[] AslanFemalePersonalStarts = ["A", "Eia", "Hia", "Mia", "Nia", "Ria", "Sia", "Tia", "Yra", "Zia"];
    private static readonly string[] AslanFemalePersonalEnds = ["a", "ei", "ha", "lia", "mi", "na", "ra", "sai", "ya", "zi"];
    private static readonly string[] VargrClanStarts = ["Ark", "Garr", "Korg", "Rukh", "Skav", "Thrag", "Urr", "Vark", "Wroth", "Zekk"];
    private static readonly string[] VargrClanEnds = ["ak", "arr", "ekh", "gorr", "kesh", "or", "rukh", "var", "vok", "zhan"];
    private static readonly string[] VargrPersonalStarts = ["Ak", "Brak", "Dorr", "Geth", "Irr", "Kav", "Nok", "Ress", "Tek", "Vek"];
    private static readonly string[] VargrPersonalEnds = ["a", "ak", "esh", "ik", "ok", "or", "ra", "rek", "sha", "uk"];
    private static readonly string[] VargrFemalePersonalStarts = ["Ava", "Eri", "Isha", "Kira", "Lura", "Mira", "Niva", "Risha", "Siva", "Vira"];
    private static readonly string[] VargrFemalePersonalEnds = ["a", "ae", "esh", "ia", "ka", "na", "ra", "sha", "va", "ya"];

    public static void Generate(Character character)
    {
        for (var attempt = 0; attempt < MaximumDuplicateRetries; attempt++)
        {
            GenerateOnce(character);

            if (!CharacterFileService.FinalCharacterExists(character))
            {
                return;
            }
        }
    }

    private static void GenerateOnce(Character character)
    {
        switch (character.Race)
        {
            case RaceType.Aslan:
                GenerateAslan(character);
                break;
            case RaceType.Vargr:
                GenerateVargr(character);
                break;
            default:
                GenerateHuman(character);
                break;
        }
    }

    private static void GenerateHuman(Character character)
    {
        var heritage = character.Heritage ?? HeritageType.Anglo;
        var heritageFirstNames = character.Gender == GenderType.Female
            ? HeritageFemaleFirstNames[heritage]
            : HeritageMaleFirstNames[heritage];
        var commonFirstNames = character.Gender == GenderType.Female
            ? CommonFemaleFirstNames
            : CommonMaleFirstNames;

        character.HumanFirstName = PickHumanGivenName(heritage, heritageFirstNames, commonFirstNames);
        character.HumanMiddleName = Random.Shared.Next(100) < 95
            ? PickHumanGivenName(heritage, heritageFirstNames, commonFirstNames)
            : string.Empty;
        character.HumanLastName = PickWeighted(HeritageLastNames[heritage], CommonLastNames);
        var suffixChance = character.Gender == GenderType.Female ? 2 : 8;
        character.HumanSuffix = Random.Shared.Next(100) < suffixChance ? Pick(HumanSuffixes) : string.Empty;
    }

    private static void GenerateAslan(Character character)
    {
        character.AslanFamilyName = BuildSyllableName(AslanFamilyStarts, AslanFamilyEnds);
        character.AslanPersonalName = character.Gender == GenderType.Female
            ? BuildSyllableName(AslanFemalePersonalStarts, AslanFemalePersonalEnds)
            : BuildSyllableName(AslanPersonalStarts, AslanPersonalEnds);
    }

    private static void GenerateVargr(Character character)
    {
        character.VargrClanName = BuildSyllableName(VargrClanStarts, VargrClanEnds);
        character.VargrRole = Pick(Enum.GetValues<VargrRoleType>());
        character.VargrPersonalName = character.Gender == GenderType.Female
            ? BuildSyllableName(VargrFemalePersonalStarts, VargrFemalePersonalEnds)
            : BuildSyllableName(VargrPersonalStarts, VargrPersonalEnds);
    }

    private static string PickHumanGivenName(
        HeritageType heritage,
        string[] heritageValues,
        string[] commonValues)
    {
        if (Random.Shared.Next(100) < 35)
        {
            return BuildHumanPhoneticName(heritage);
        }

        return PickWeighted(heritageValues, commonValues);
    }

    private static string BuildHumanPhoneticName(HeritageType heritage)
    {
        var phonetics = HeritagePhonetics[heritage];
        return $"{Pick(phonetics.Starts)}{Pick(phonetics.Ends)}";
    }

    private static string PickWeighted(string[] heritageValues, string[] commonValues)
    {
        return Random.Shared.Next(100) < 75 ? Pick(heritageValues) : Pick(commonValues);
    }

    private static string BuildName(string[] starts, string[] ends)
    {
        return $"{Pick(starts)}{Pick(ends)}";
    }

    private static string BuildSyllableName(string[] starts, string[] ends)
    {
        return Random.Shared.Next(100) < 35
            ? $"{Pick(starts)}{Pick(ends)}{Pick(ends)}"
            : BuildName(starts, ends);
    }

    private static T Pick<T>(IReadOnlyList<T> values)
    {
        return values[Random.Shared.Next(values.Count)];
    }

    private sealed record NamePhonetics(string[] Starts, string[] Ends);
}
