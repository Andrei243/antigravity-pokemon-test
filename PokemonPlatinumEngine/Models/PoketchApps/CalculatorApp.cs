using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>
/// The calculator (<c>calculator/main.c</c>, its arithmetic <c>calculator/value.c</c>): ten places on its display,
/// the four operations, a decimal point, and a cry for a sum that is the number of a Pokémon seen.
///
/// The original keeps every number as a whole significand and a count of decimal places, and works the four
/// operations on those as <c>CalculatorValue_Add</c>, <c>_Subtract</c>, <c>_Multiply</c> and <c>_Divide</c> do,
/// quirks and all: a product that runs past 64 bits wraps, a quotient stops at the places that still fit, and
/// anything longer than ten places (the minus sign and the point each taking one) shows the error across the
/// display (<c>CalculatorValue_CanBeDisplayed</c>). The display is redrawn only where the original starts a
/// graphics task, so it can show a number the calculator has already let go of, as the original's does.
/// </summary>
public sealed class CalculatorApp : PoketchAppState
{
    /// <summary>The places on the display (<c>CALCULATOR_MAX_DIGITS</c>).</summary>
    public const int MaxDigits = 10;

    /// <summary>The buttons, by the original's numbers (<c>CALC_BUTTON_*</c>): 0 to 9 are the digits.</summary>
    public const int Decimal = 10, Minus = 11, Plus = 12, Times = 13, Divide = 14, EqualsKey = 15, Clear = 16, None = 17;

    /// <summary>What a place of the display shows besides a digit (<c>DISPLAY_SYMBOL_*</c>).</summary>
    public const int PointSymbol = 10, NegativeSymbol = 11, InvalidSymbol = 12;

    /// <summary>
    /// The keys in blocks: four rows of five keys 8 by 6 under the display, a block apart, the 0, C and = keys two
    /// wide, laid out as the original's (<c>Init</c>'s hit boxes).
    /// </summary>
    public static readonly PoketchButton[] Keys = MakeKeys();

    private static PoketchButton[] MakeKeys()
    {
        static PoketchButton Key(int id, int col, int row, int cols = 1) => new(id, 1 + col * 9, 9 + row * 7, cols * 9 - 1, 6);
        return new[]
        {
            Key(7, 0, 0), Key(8, 1, 0), Key(9, 2, 0), Key(Clear, 3, 0, 2),
            Key(4, 0, 1), Key(5, 1, 1), Key(6, 2, 1), Key(Plus, 3, 1), Key(Minus, 4, 1),
            Key(1, 0, 2), Key(2, 1, 2), Key(3, 2, 2), Key(Times, 3, 2), Key(Divide, 4, 2),
            Key(0, 0, 3, 2), Key(Decimal, 2, 3), Key(EqualsKey, 3, 3, 2),
        };
    }

    private enum Step { Operand1, Operator, Operand2, Result, Error }

    private readonly Value operand1 = new(), operand2 = new(), result = new();
    private int pendingOperation = None;
    private bool decimalActive;
    private Step step = Step.Operand1;
    private List<int> shown = new() { 0 };

    public override PoketchApp App => PoketchApp.Calculator;

    /// <summary>What the display shows, its places from the left (right-aligned in the ten): digits 0 to 9, <see cref="PointSymbol"/>, <see cref="NegativeSymbol"/> and <see cref="InvalidSymbol"/>.</summary>
    public IReadOnlyList<int> Display => shown;

    /// <summary>The operation shown beside the number (<see cref="Plus"/> and the rest), or <see cref="None"/>.</summary>
    public int ShownOperator { get; private set; } = None;

    /// <summary>The display as text: "12.5", "-3", or "E" in all ten places for the error.</summary>
    public string Text
    {
        get
        {
            var text = new StringBuilder();
            foreach (int s in shown) text.Append(s switch { PointSymbol => '.', NegativeSymbol => '-', InvalidSymbol => 'E', _ => (char)('0' + s) });
            return text.ToString();
        }
    }

    /// <summary>The key last touched, and the seconds left of it shown pressed in.</summary>
    public int PressedKey { get; private set; } = None;
    public float PressedFor { get; private set; }

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => Keys;

    public override void Update(float dt, PoketchContext context) => PressedFor = Math.Max(0f, PressedFor - dt);

    public override void Press(int button, PoketchContext context)
    {
        if (button < 0 || button >= None) return;
        // Task_PressButton: every key clicks as it goes down
        context.Sound("poketch");
        PressedKey = button;
        PressedFor = 0.15f;
        switch (step)
        {
            case Step.Operand1: EnterOperand1(button, context); break;
            case Step.Operator: EnterOperator(button, context); break;
            case Step.Operand2: EnterOperand2(button, context); break;
            case Step.Result: ShowResult(button, context); break;
            case Step.Error: ShowValueError(button); break;
        }
    }

    private static bool IsOperation(int b) => b is Plus or Minus or Times or Divide;

    // State_EnterOperand1
    private void EnterOperand1(int b, PoketchContext context)
    {
        if (b == Clear)
        {
            Reset();
            ShowClearOutput();
        }
        else if (b == Decimal) decimalActive = true;
        else if (IsOperation(b))
        {
            pendingOperation = b;
            decimalActive = false;
            ShownOperator = pendingOperation;
            step = Step.Operator;
        }
        else if (b == EqualsKey)
        {
            PlayCry(operand1, context);
            operand1.Clear();
        }
        else if (decimalActive ? operand1.AppendDecimalDigit(b) : operand1.AppendDigit(b)) ShowValue(operand1);
    }

    // State_EnterOperator
    private void EnterOperator(int b, PoketchContext context)
    {
        if (b == Clear)
        {
            Reset();
            ShowClearOutput();
            step = Step.Operand1;
        }
        else if (b == Decimal)
        {
            operand2.Clear();
            ShowValue(operand2);
            decimalActive = true;
            step = Step.Operand2;
        }
        else if (IsOperation(b))
        {
            pendingOperation = b;
            ShownOperator = pendingOperation;
        }
        else if (b == EqualsKey)
        {
            // The number entered is taken as the second operand too: 3 + = is 6
            operand2.CopyFrom(operand1);
            if (Evaluate(pendingOperation))
            {
                PlayCry(result, context);
                ShowResult();
                step = Step.Result;
            }
            else ShowInvalid();
        }
        else
        {
            operand2.Clear();
            if (operand2.AppendDigit(b))
            {
                ShowValue(operand2);
                step = Step.Operand2;
            }
        }
    }

    // State_EnterOperand2
    private void EnterOperand2(int b, PoketchContext context)
    {
        if (b == Clear)
        {
            Reset();
            ShowClearOutput();
            step = Step.Operand1;
        }
        else if (b == Decimal) decimalActive = true;
        else if (IsOperation(b))
        {
            if (Evaluate(pendingOperation))
            {
                pendingOperation = b;
                decimalActive = false;
                operand1.CopyFrom(result);
                ShowValue(result);
                ShownOperator = pendingOperation;
                step = Step.Operator;
            }
            else ShowInvalid();
        }
        else if (b == EqualsKey)
        {
            if (Evaluate(pendingOperation))
            {
                PlayCry(result, context);
                ShowResult();
                decimalActive = false;
                step = Step.Result;
            }
            else ShowInvalid();
        }
        else if (decimalActive ? operand2.AppendDecimalDigit(b) : operand2.AppendDigit(b)) ShowValue(operand2);
    }

    // State_ShowResult
    private void ShowResult(int b, PoketchContext context)
    {
        if (b == Clear)
        {
            Reset();
            ShowClearOutput();
            step = Step.Operand1;
        }
        else if (b == Decimal)
        {
            Reset();
            ShowValue(operand1);
            decimalActive = true;
            step = Step.Operand1;
        }
        else if (IsOperation(b))
        {
            pendingOperation = b;
            ShownOperator = pendingOperation;
            decimalActive = false;
            operand1.CopyFrom(result);
            step = Step.Operator;
        }
        else if (b == EqualsKey)
        {
            // = again repeats the last operation on the result: 2 + 3 = = is 8
            operand1.CopyFrom(result);
            if (Evaluate(pendingOperation))
            {
                PlayCry(result, context);
                ShowResult();
                decimalActive = false;
            }
            else ShowInvalid();
        }
        else
        {
            operand1.Clear();
            if (operand1.AppendDigit(b)) ShowValue(operand1);
            step = Step.Operand1;
        }
    }

    // State_ShowValueError: only C, the point or a digit go on from the error
    private void ShowValueError(int b)
    {
        if (b == Clear)
        {
            Reset();
            ShowValue(operand1);
            step = Step.Operand1;
        }
        else if (b == Decimal)
        {
            Reset();
            ShowValue(operand1);
            decimalActive = true;
            step = Step.Operand1;
        }
        else if (b < Decimal)
        {
            Reset();
            if (operand1.AppendDigit(b)) ShowValue(operand1);
            step = Step.Operand1;
        }
    }

    // ResetCalculatorState
    private void Reset()
    {
        operand1.Clear();
        operand2.Clear();
        result.Clear();
        pendingOperation = None;
        decimalActive = false;
    }

    // EvaluateResult
    private bool Evaluate(int operation)
    {
        switch (operation)
        {
            case Plus: Value.Add(operand1, operand2, result); break;
            case Minus: Value.Subtract(operand1, operand2, result); break;
            case Times: Value.Multiply(operand1, operand2, result); break;
            case Divide: Value.Divide(operand1, operand2, result); break;
        }
        return result.CanBeDisplayed();
    }

    // The graphics tasks that redraw the display: Task_DisplayOperand1/2, Task_ClearOutput, Task_DisplayResult,
    // Task_DisplayInvalidValue. A value that can't be put into symbols leaves the display as it was
    // (CalculatorValue_GetDisplaySymbols returning FALSE over the buffer it didn't fill).
    private void ShowValue(Value value)
    {
        if (value.DisplaySymbols() is { } symbols) shown = symbols;
    }

    private void ShowClearOutput()
    {
        ShowValue(operand1);
        ShownOperator = None;
    }

    private void ShowResult()
    {
        ShowValue(result);
        ShownOperator = None;
    }

    private void ShowInvalid()
    {
        shown = Enumerable.Repeat(InvalidSymbol, MaxDigits).ToList();
        ShownOperator = None;
        step = Step.Error;
    }

    /// <summary>
    /// <c>PlayResultSpeciesCry</c>: a whole part from 1 to 493 is a Pokémon's number, in the National Pokédex once
    /// the player has it and in Sinnoh's before; that Pokémon's cry plays if the Pokédex has seen it.
    /// </summary>
    private static void PlayCry(Value value, PoketchContext context)
    {
        const int nationalDexCount = 493;
        long number = value.SignedInt();
        if (number <= 0 || number > nationalDexCount || context.Pokedex is not { } dex) return;
        int species = dex.NationalUnlocked
            ? (int)number
            : Pokedex.Entries(PokedexMode.Sinnoh).FirstOrDefault(e => e.Number == number).Species?.DexNumber ?? 0;
        if (species <= 0 || species > nationalDexCount || !dex.IsSeen(species)) return;
        if (PokemonDatabase.GetByDex(species) is not { } kind) return;
        // A Pokémon made only to be heard: its own fixed generator, so no chance of the field's is drawn for it
        context.Cry(new Pokemon(kind, 5, new Random(0)));
    }

    /// <summary>
    /// A number as the original's calculator keeps it (<c>CalculatorValue</c>): a 64-bit significand, the places
    /// after the point, a sign and whether a division by nought made it no number at all.
    /// </summary>
    internal sealed class Value
    {
        public ulong Significand;
        public int DecimalPlaces;
        public bool Negative;
        public bool Invalid;

        private static ulong Pow10(int n)
        {
            ulong p = 1;
            for (int i = 0; i < n; i++) p = unchecked(p * 10);
            return p;
        }

        public void Clear()
        {
            Significand = 0;
            Negative = false;
            DecimalPlaces = 0;
            Invalid = false;
        }

        public void CopyFrom(Value other)
        {
            Significand = other.Significand;
            DecimalPlaces = other.DecimalPlaces;
            Negative = other.Negative;
            Invalid = other.Invalid;
        }

        private Value Copy()
        {
            var v = new Value();
            v.CopyFrom(this);
            return v;
        }

        public static void Add(Value a, Value b, Value result)
        {
            if (!a.Negative && b.Negative)
            {
                var negated = b.Copy();
                negated.Negative = false;
                Subtract(a, negated, result);
                return;
            }
            if (a.Negative && !b.Negative)
            {
                var negated = a.Copy();
                negated.Negative = false;
                Subtract(b, negated, result);
                return;
            }
            var (x, y) = Align(a, b);
            result.Significand = unchecked(x.Significand + y.Significand);
            result.DecimalPlaces = x.DecimalPlaces;
            result.Negative = x.Negative;
            result.Simplify();
        }

        public static void Subtract(Value a, Value b, Value result)
        {
            if (a.Negative != b.Negative)
            {
                var negated = b.Copy();
                negated.Negative = !negated.Negative;
                Add(a, negated, result);
                return;
            }
            if (IsEqual(a, b))
            {
                result.Clear();
                return;
            }
            var (x, y) = Align(a, b);
            if ((x.Significand < y.Significand) ^ x.Negative)
            {
                (x, y) = (y, x);
                result.Negative = !x.Negative;
            }
            else result.Negative = x.Negative;
            result.Significand = unchecked(x.Significand - y.Significand);
            result.DecimalPlaces = x.DecimalPlaces;
            result.Simplify();
        }

        public static void Multiply(Value a, Value b, Value result)
        {
            result.Significand = unchecked(a.Significand * b.Significand);
            result.DecimalPlaces = a.DecimalPlaces + b.DecimalPlaces;
            result.Negative = a.Negative ^ b.Negative;
            result.Simplify();
        }

        public static void Divide(Value a, Value b, Value result)
        {
            if (b.Significand == 0)
            {
                result.Invalid = true;
                return;
            }
            var (dividend, divisor) = Align(a, b);
            result.Significand = dividend.Significand / divisor.Significand;
            result.DecimalPlaces = 0;
            // The length is taken before the sign is set, with whatever sign the result had last (as the original)
            int length = result.LengthLeftOfDecimal();
            ulong remainder = dividend.Significand % divisor.Significand;
            while (remainder != 0)
            {
                if (length + 1 + result.DecimalPlaces >= MaxDigits) break;
                remainder = unchecked(remainder * 10);
                result.Significand = unchecked(result.Significand * 10 + remainder / divisor.Significand);
                result.DecimalPlaces++;
                remainder %= divisor.Significand;
            }
            result.Negative = dividend.Negative ^ divisor.Negative;
        }

        public bool AppendDigit(int digit)
        {
            if (Length() >= MaxDigits) return false;
            // A digit after the point that isn't entered as one changes nothing, but the display is redrawn
            if (DecimalPlaces == 0) Significand = unchecked(Significand * 10 + (ulong)digit);
            return true;
        }

        public bool AppendDecimalDigit(int digit)
        {
            if (Length() >= MaxDigits) return false;
            Significand = unchecked(Significand * 10 + (ulong)digit);
            DecimalPlaces++;
            return true;
        }

        private static bool IsEqual(Value a, Value b)
        {
            if (a.Significand != b.Significand || a.DecimalPlaces != b.DecimalPlaces) return false;
            if (a.Negative != b.Negative && a.Significand != 0) return false;
            return true;
        }

        public bool CanBeDisplayed()
        {
            if (Invalid) return false;
            int length = LengthLeftOfDecimal();
            if (DecimalPlaces != 0) length += 1 + DecimalPlaces;
            return length <= MaxDigits;
        }

        /// <summary><c>CalculatorValue_GetDisplaySymbols</c>; null where it returns FALSE.</summary>
        public List<int>? DisplaySymbols()
        {
            if (Invalid) return null;
            var symbols = new List<int>();
            if (Significand == 0)
            {
                symbols.Add(0);
                if (DecimalPlaces != 0)
                {
                    symbols.Add(PointSymbol);
                    for (int i = 0; i < DecimalPlaces; i++) symbols.Add(0);
                }
                return symbols;
            }

            ulong significand = Significand;
            int numDigits = 1;
            while (numDigits < 20 && significand >= Pow10(numDigits)) numDigits++;
            int total = numDigits;
            int leadingZeros = DecimalPlaces - numDigits;
            if (leadingZeros >= 0) total += 2 + leadingZeros;
            else if (DecimalPlaces != 0) total += 1;
            if (total + (Negative ? 1 : 0) > MaxDigits) return null;

            if (Negative) symbols.Add(NegativeSymbol);
            int start = symbols.Count;
            if (leadingZeros >= 0)
            {
                symbols.Add(0);
                symbols.Add(PointSymbol);
                for (int i = 0; i < leadingZeros; i++) symbols.Add(0);
            }
            else
            {
                int integerDigits = -leadingZeros;
                for (int i = 0; i < integerDigits; i++)
                {
                    ulong p = Pow10(numDigits - 1 - i);
                    symbols.Add((int)(significand / p));
                    significand %= p;
                }
                if (DecimalPlaces != 0) symbols.Add(PointSymbol);
            }
            int written = symbols.Count - start;
            if (written < total)
            {
                int fractional = total - written;
                for (int i = 0; i < fractional; i++)
                {
                    ulong p = Pow10(fractional - 1 - i);
                    symbols.Add((int)(significand / p));
                    significand %= p;
                }
            }
            return symbols;
        }

        public long SignedInt()
        {
            long integer = (long)(Significand / Pow10(DecimalPlaces));
            return Negative ? -integer : integer;
        }

        private int LengthLeftOfDecimal()
        {
            ulong power = 10;
            int length = 1;
            while (Significand >= power)
            {
                if (++length > MaxDigits) break;
                power *= 10;
            }
            length = length > DecimalPlaces ? length - DecimalPlaces : 1;
            return length + (Negative ? 1 : 0);
        }

        private int Length()
        {
            int length = LengthLeftOfDecimal();
            if (DecimalPlaces != 0) length += 1 + DecimalPlaces;
            return length;
        }

        // Simplify: trailing noughts after the point dropped, then places cut off the end until it fits
        private void Simplify()
        {
            if (DecimalPlaces != 0)
            {
                while (Significand % 10 == 0)
                {
                    Significand /= 10;
                    if (--DecimalPlaces == 0) break;
                }
            }
            int length = LengthLeftOfDecimal();
            if (DecimalPlaces != 0)
            {
                length += 1 + DecimalPlaces;
                if (length > MaxDigits)
                {
                    int extra = length - MaxDigits;
                    if (extra <= DecimalPlaces)
                    {
                        Significand /= Pow10(extra);
                        DecimalPlaces -= extra;
                    }
                }
            }
        }

        // AlignDecimalPoints: the one with fewer places is multiplied up to the other's
        private static (Value, Value) Align(Value a, Value b)
        {
            var x = a.Copy();
            var y = b.Copy();
            var (more, fewer) = x.DecimalPlaces < y.DecimalPlaces ? (y, x) : (x, y);
            int padding = more.DecimalPlaces - fewer.DecimalPlaces;
            fewer.Significand = unchecked(fewer.Significand * Pow10(padding));
            fewer.DecimalPlaces = more.DecimalPlaces;
            return (x, y);
        }
    }
}
