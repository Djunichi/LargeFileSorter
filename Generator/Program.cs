using System.Text;

Console.WriteLine("Set file size in Mb");

float.TryParse(Console.ReadLine(), out var size);

Console.WriteLine("Start file generating...");

const string fileName = "../../../../text.txt";

var words = new List<string>()
{
    "Apple", "Ant", "Art",
    "Bug", "Bunny", "Bee",
    "Cat", "Cold", "Count",
    "Dog", "Delta", "Deer",
    "Element", "Elephant", "Enemy",
    "For", "Focus", "File",
    "Gem", "Good", "Goal",
    "Horse", "House", "Hour",
    "Ice", "Idle", "Insect",
    "Join", "Jungle", "Joy",
    "King", "Kind", "Kangaroo",
    "Lemon", "Lost", "Less",
    "Moon", "More", "Mess",
    "Nice", "Need", "Noon",
    "Oak", "Odd", "Old",
    "Perfect", "Punish", "Pony",
    "Queen", "Queue", "Quarter",
    "Rabbit", "Rack", "Root",
    "Sun", "Soon", "Shine",
    "To", "Ten", "Teeth",
    "Union", "User", "Uber",
    "Violet", "Vector", "Vehicle",
    "Web", "Windows", "Walk",
    "X-ray", "Xenon", "Xerox",
    "Yacht", "Yummy", "Yard",
    "Zebra", "Zen", "Zoo"
    
};

using var writer = 
    new StreamWriter(
        new BufferedStream(File.OpenWrite(fileName), 1024 * 1024)
    );

var rnd = new Random();
var strToWrite = new StringBuilder();

var filesize = BytesToMb(new FileInfo(fileName).Length);

while (filesize < size)
{
    strToWrite.Append(rnd.Next(0, 1000));
    strToWrite.Append(".");

    for (int i = 0; i < rnd.Next(1, 5); i++)
    {
        strToWrite.Append(" " + words[rnd.Next(0, words.Count - 1)]);
    }

    writer.WriteLine(strToWrite);

    strToWrite.Clear();

    filesize = BytesToMb(new FileInfo(fileName).Length);
}

Console.WriteLine("Generating completed");

float BytesToMb(float bytes)
{
    return bytes / 1024f / 1024f;
}