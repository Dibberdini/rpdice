using System.CommandLine;

namespace rpdice;

class RPDice
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Calculator for Role-Playing dice");

        var diceArgument = new Argument<string>(
            name: "dice input",
            description: "input any value and print it out");
        diceArgument.SetDefaultValue("empty");
        rootCommand.AddArgument(diceArgument);

        var armorOption = new Option<int>(
            name: "--ac",
            description: "The armor class of the target",
            getDefaultValue: () => 10);
        armorOption.AddAlias("-ac");

        var advantageOption = new Option<int>(
            name: "--advantage",
            description: "If the roll has advantage [values -1,0,1]",
            getDefaultValue: () => 0);
        advantageOption.AddAlias("-adv");

        var hitCommand = new Command("hit", "Calculate chance of hitting enemy");
        hitCommand.AddOption(armorOption);
        hitCommand.AddOption(advantageOption);
        hitCommand.AddArgument(diceArgument);
        rootCommand.AddCommand(hitCommand);

        var avgCommand = new Command("avg", "Calculate the average roll");
        avgCommand.AddArgument(diceArgument);
        rootCommand.AddCommand(avgCommand);

        rootCommand.SetHandler((diceArgument) =>
            {
                if (diceArgument == "empty") return;
                rollDice(diceArgument);
            }, diceArgument);

        hitCommand.SetHandler((diceArgument, armorOption, advantageOption) =>
            {
                if (diceArgument == "empty") return;
                CalculateHit(diceArgument, armorOption, advantageOption);
            }, diceArgument, armorOption, advantageOption);

        avgCommand.SetHandler((diceArgument) =>
            {
                if (diceArgument == "empty") return;
                CalculateDice(diceArgument);
            }, diceArgument);

        return await rootCommand.InvokeAsync(args);
    }

    static void CalculateDice(string input)
    {
        try
        {
            DiceSet dice = parseDice(input);
            double averageRoll = 0;
            foreach (var Die in dice.diceList)
            {
                averageRoll += Die.amount * (Die.value / 2 + 0.5);
            }
            averageRoll += dice.modifier;

            Console.WriteLine(averageRoll);
        }
        catch (Exception)
        {
            Console.WriteLine("Invalid Input");
        }
    }

    static void CalculateHit(string input, int ac, int adv)
    {
        if (!input.Contains('d')) input = "1d0" + input;
        try
        {
            DiceSet dice = parseDice(input);
            double averageRoll = 0;
            foreach (var Die in dice.diceList)
            {
                averageRoll += Die.amount * (Die.value / 2 + 0.5);
            }
            averageRoll += dice.modifier;
            int clampedChance = 0;
            double chanceToHit = 0;
            //Take advantage into account. There is no 0% or 100% because of nat 20's and 1's.
            if (adv == 1)
            {
                //Formula for advantage-rolls only works if the bonus(+1) is not equal to or greater than target AC.
                if (averageRoll + 1 < ac)
                {
                    chanceToHit = 1 - (Math.Pow((ac - averageRoll - 1), 2) / 400);
                }
                else
                {
                    chanceToHit = 100;
                }
                clampedChance = Clamp((int)Math.Round(chanceToHit * 100), 10, 99);
            }
            else if (adv == -1)
            {
                //Formula for disadvantage-rolls breaks down if there is a large enough negative bonus.
                if (21 + averageRoll - ac > 0)
                {
                    chanceToHit = Math.Pow((21 + averageRoll - ac), 2) / 400;
                }
                else
                {
                    chanceToHit = 0;
                }
                clampedChance = Clamp((int)Math.Round(chanceToHit * 100), 1, 90);
            }
            else
            {
                chanceToHit = (21 + averageRoll - ac) / 20;
                clampedChance = Clamp((int)Math.Round(chanceToHit * 100), 5, 95);
            }
            Console.WriteLine(clampedChance + "%");
        }
        catch (Exception)
        {
            Console.WriteLine("Invalid Input");
        }
    }

    static void rollDice(string input)
    {
        Random rng = new Random();
        try
        {
            DiceSet dice = parseDice(input);
            int result = 0;
            foreach (var die in dice.diceList)
            {
                for (int i = 0; i < die.amount; i++)
                {
                    int rValue = rng.Next(1, die.value + 1);
                    result += rValue;
                }
            }
            result += dice.modifier;
            Console.WriteLine(result);
        }
        catch (Exception)
        {
            Console.WriteLine("Invalid Input");
        }
    }

    static DiceSet parseDice(string input)
    {
        DiceSet dice = new();
        var diceInput = input.ToLower();

        string[] subs = diceInput.Split('-');
        diceInput = String.Join("+-", subs);

        if (!diceInput.Contains('d'))
        {
            throw new ArgumentException("Invalid input.");
        }
        string[] splits = diceInput.Split('+');

        foreach (string item in splits)
        {
            if (item.Contains('d'))
            {
                var die = item.Split('d');
                dice.AddDie(Int32.Parse(die[0]), Int32.Parse(die[1]));
            }
            else
            {
                dice.modifier += Int32.Parse(item);
            }
        }

        return dice;
    }

    static int Clamp(int value, int min, int max)
    {
        if (value > max) return max;
        if (value < min) return min;
        return value;
    }
}

class DiceSet
{
    public List<Die> diceList = new List<Die>();
    public int modifier;

    public DiceSet()
    {
        modifier = 0;
    }

    public void AddDie(int amount, int value)
    {
        diceList.Add(new Die(amount, value));
    }
}

class Die(int dieAmount, int dieValue)
{
    public int amount = dieAmount;
    public int value = dieValue;
}