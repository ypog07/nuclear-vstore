﻿using System;
using System.Linq;

using NuClear.VStore.Descriptors.Objects;
using NuClear.VStore.Descriptors.Templates;
using NuClear.VStore.Objects.ContentValidation;
using NuClear.VStore.Objects.ContentValidation.Errors;

using Xunit;

// ReSharper disable UnusedMember.Global
namespace VStore.UnitTests
{
    public class FormattedTextValidationTests
    {
        private static readonly TestHelpers.Validator[] AllChecks =
            {
                FormattedTextValidator.CheckLength,
                FormattedTextValidator.CheckWordsLength,
                FormattedTextValidator.CheckLinesCount,
                FormattedTextValidator.CheckRestrictedSymbols,
                FormattedTextValidator.CheckValidHtml,
                FormattedTextValidator.CheckSupportedHtmlTags,
                FormattedTextValidator.CheckAttributesAbsence,
                FormattedTextValidator.CheckEmptyList,
                FormattedTextValidator.CheckNestedList,
                FormattedTextValidator.CheckUnsupportedListElements
            };

        [Fact]
        public void TestTextCheckLength()
        {
            const int MaxSymbols = 10;
            var value = new TextElementValue { Raw = new string('a', MaxSymbols) };
            var constraints = new TextElementConstraints { IsFormatted = true, MaxSymbols = MaxSymbols };

            var error = TestHelpers.MakeCheck<TextElementValue, ElementTextTooLongError>(
                value,
                constraints,
                FormattedTextValidator.CheckLength,
                val => val.Raw = new string('b', MaxSymbols + 1));
            Assert.Equal(MaxSymbols, error.MaxLength);
            Assert.Equal(value.Raw.Length, error.ActualLength);

            value.Raw = "<i><b>" + new string('a', MaxSymbols) + "</b></i>";
            error = TestHelpers.MakeCheck<TextElementValue, ElementTextTooLongError>(
                value,
                constraints,
                FormattedTextValidator.CheckLength,
                val => val.Raw = "<i><b>" + new string('b', MaxSymbols + 1) + "</b></i>");
            Assert.Equal(MaxSymbols, error.MaxLength);
            Assert.Equal(MaxSymbols + 1, error.ActualLength);
            Assert.Equal(ElementValidationErrors.TextTooLong, error.ErrorType);
        }

        [Fact]
        public void TestTextCheckLongWords()
        {
            const int MaxSymbols = 10;
            var value = new TextElementValue { Raw = new string('a', MaxSymbols) };
            var constraints = new TextElementConstraints { IsFormatted = true, MaxSymbolsPerWord = MaxSymbols };

            var error = TestHelpers.MakeCheck<TextElementValue, ElementWordsTooLongError>(
                value,
                constraints,
                FormattedTextValidator.CheckWordsLength,
                val => val.Raw = new string('b', MaxSymbols + 1));
            Assert.Equal(MaxSymbols, error.MaxWordLength);
            Assert.Equal(1, error.TooLongWords.Count);
            Assert.Equal(value.Raw, error.TooLongWords.First());
            Assert.Equal(ElementValidationErrors.WordsTooLong, error.ErrorType);
        }

        [Fact]
        public void TestTextCheckMaxLines()
        {
            var value = new TextElementValue { Raw = "1 <br> 2 <br/> 3 <br /> 4 \n 4" };
            var constraints = new TextElementConstraints { IsFormatted = true, MaxLines = 4 };

            var error = TestHelpers.MakeCheck<TextElementValue, TooManyLinesError>(
                value,
                constraints,
                FormattedTextValidator.CheckLinesCount,
                val => val.Raw += "<ul><li> 5 </li></ul>");
            Assert.Equal(constraints.MaxLines, error.MaxLinesCount);
            Assert.Equal(constraints.MaxLines + 1, error.ActualLinesCount);
            Assert.Equal(ElementValidationErrors.TooManyLines, error.ErrorType);
        }

        [Fact]
        public void TestTextCheckRestrictedSymbols()
        {
            const string AllChars = "abcdefghijklmnopqrstuvwxyz \n\t абвгдеёжзийклмнопрстуфхцчшщьыъэюя 1234567890 \\ \" .,;:~'`!? №@#$%^&|_ []{}()<> /*-+=";
            var value = new TextElementValue { Raw = AllChars };
            var constraints = new TextElementConstraints { IsFormatted = true };

            var errorOnSpace = TestHelpers.MakeCheck<TextElementValue, NonBreakingSpaceSymbolError>(
                value,
                constraints,
                FormattedTextValidator.CheckRestrictedSymbols,
                val => val.Raw = "\x00A0");
            Assert.Equal(ElementValidationErrors.NonBreakingSpaceSymbol, errorOnSpace.ErrorType);

            value.Raw = AllChars.ToUpper();
            var errorOnChars = TestHelpers.MakeCheck<TextElementValue, ControlСharactersInTextError>(
                value,
                constraints,
                FormattedTextValidator.CheckRestrictedSymbols,
                val => val.Raw = "\r");
            Assert.Equal(ElementValidationErrors.ControlСharacters, errorOnChars.ErrorType);
        }

        [Fact]
        public void TestTextCheckMarkup()
        {
            var value = new TextElementValue { Raw = "<br/><br /><br><ul><li><b><i><strong> text &nbsp; </strong><i/></b></li><em>small</em><li></li></ul>" };
            var constraints = new TextElementConstraints { IsFormatted = true };

            var error = TestHelpers.MakeCheck<TextElementValue, InvalidHtmlError>(
                value,
                constraints,
                FormattedTextValidator.CheckValidHtml,
                val => val.Raw += "<ul>");
            Assert.Equal(ElementValidationErrors.InvalidHtml, error.ErrorType);
        }

        [Fact]
        public void TestTextCheckTags()
        {
            var value = new TextElementValue { Raw = "<br/><br /><br><ul><li><b><i><strong> text &nbsp; </strong><i/></b></li><em>small</em><li></li></ul>" };
            var constraints = new TextElementConstraints { IsFormatted = true };

            var error = TestHelpers.MakeCheck<TextElementValue, UnsupportedTagsError>(
                value,
                constraints,
                FormattedTextValidator.CheckSupportedHtmlTags,
                val => val.Raw = "<html><head></head><body><p></p><hr></body></html>");
            Assert.Equal(5, error.UnsupportedTags.Count);
            Assert.Equal(error.UnsupportedTags, new[] { "html", "head", "body", "p", "hr" });
            Assert.Equal(ElementValidationErrors.UnsupportedTags, error.ErrorType);
        }

        [Fact]
        public void TestTextCheckAttributes()
        {
            var value = new TextElementValue { Raw = "<b></b>" };
            var constraints = new TextElementConstraints { IsFormatted = true };

            var error = TestHelpers.MakeCheck<TextElementValue, UnsupportedAttributesError>(
                value,
                constraints,
                FormattedTextValidator.CheckAttributesAbsence,
                val => val.Raw = "<b class='err'><i onclick='alert(123)'></i></b>");

            Assert.Equal(2, error.UnsupportedAttributes.Count);
            Assert.Equal(error.UnsupportedAttributes, new[] { "class", "onclick" });
            Assert.Equal(ElementValidationErrors.UnsupportedAttributes, error.ErrorType);
        }

        [Fact]
        public void TestTextCheckEmptyList()
        {
            var value = new TextElementValue { Raw = "<ul><li></li></ul>" };
            var constraints = new TextElementConstraints { IsFormatted = true };

            var error = TestHelpers.MakeCheck<TextElementValue, EmptyListError>(
                value,
                constraints,
                FormattedTextValidator.CheckEmptyList,
                val => val.Raw = "<ul> </ul>");

            Assert.Equal(ElementValidationErrors.EmptyList, error.ErrorType);
        }

        [Fact]
        public void TestTextCheckNestedList()
        {
            var value = new TextElementValue { Raw = "<ul><li> list item </li></ul>" };
            var constraints = new TextElementConstraints { IsFormatted = true };

            var error = TestHelpers.MakeCheck<TextElementValue, NestedListError>(
                value,
                constraints,
                FormattedTextValidator.CheckNestedList,
                val => val.Raw = "<ul><li> outer list <ul><li> inner list </li></ul> </li></ul>");

            Assert.Equal(ElementValidationErrors.NestedList, error.ErrorType);
        }

        [Fact]
        public void TestTextCheckListElements()
        {
            var value = new TextElementValue { Raw = "<ul><li> list item </li></ul>" };
            var constraints = new TextElementConstraints { IsFormatted = true };

            var error = TestHelpers.MakeCheck<TextElementValue, UnsupportedListElementsError>(
                value,
                constraints,
                FormattedTextValidator.CheckUnsupportedListElements,
                val => val.Raw = "<ul><hr></ul>");

            Assert.Equal(ElementValidationErrors.UnsupportedListElements, error.ErrorType);
        }

        [Theory]
        [InlineData("Too long text", 1, null, null, false, 1)]
        [InlineData("<i><b>Too long text</b></i>", 1, null, null, false, 1)]
        [InlineData("Too_long_word", null, 1, null, false, 1)]
        [InlineData("<i><b>Too_long_word</b></i>", null, 1, null, false, 1)]
        [InlineData("Text <br> on <br/> too <br /> many \n lines", null, null, 3, false, 1)]
        [InlineData("Too_long_word_in_too_long_text", 1, 1, null, false, 2)]
        [InlineData("\r\v bad symbols and non breaking space \xA0", null, null, null, true, 2)]
        [InlineData("Too_long_word <br> on <br> too <br> many <br> lines", null, 1, 4, false, 2)]
        [InlineData("Too_long_word_in_too_long_text <br> on <br> too <br> many <br> lines", 1, 1, 4, false, 3)]
        [InlineData("Long_Word in too long text <br> with too many lines, \r\v bad symbols and non breaking space \xA0", 10, 5, 1)]
        public void TestAllChecks(string text, int? maxLength, int? maxWordLength, int? maxLines, bool containsRestrictedSymbols = true, int expectedErrorsCount = 5)
        {
            IObjectElementValue value = new TextElementValue { Raw = text };
            var constraints = new TextElementConstraints { IsFormatted = true, MaxLines = maxLines, MaxSymbols = maxLength, MaxSymbolsPerWord = maxWordLength };

            TestHelpers.InternalTextChecksTest(AllChecks, containsRestrictedSymbols, expectedErrorsCount, value, constraints);

            // FasComment cannot be formatted:
            value = new FasElementValue { Raw = "custom", Text = text };
            Assert.Throws<InvalidCastException>(() => TestHelpers.InternalTextChecksTest(AllChecks, containsRestrictedSymbols, expectedErrorsCount, value, constraints));
        }
    }
}
