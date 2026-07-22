using System.Text.Json.Nodes;
using RimworldDuplicateTexturesScanner.Models;
using RimworldDuplicateTexturesScanner.Services;

namespace RimworldDuplicateTexturesScanner.Tests;

[TestFixture]
public sealed class JsonRimSortUserRuleEditorTests
{
    [Test]
    public void AddLoadAfterRule_PreservesArrayRulesAndDocumentProperties()
    {
        using var directory = new TemporaryDirectory();
        var rulesPath = directory.CreateFile("userRules.json", """
            {
              "customProperty": "preserve this",
              "rules": [
                { "packageId": "framework.mod", "loadBefore": ["*"] }
              ]
            }
            """);
        var editor = new JsonRimSortUserRuleEditor();

        editor.Load(rulesPath);
        editor.AddLoadAfterRule("preferred.mod", [new RimSortLoadAfterTarget("first.mod", "First"), new RimSortLoadAfterTarget("second.mod", "Second")]);
        editor.Save();

        var document = JsonNode.Parse(File.ReadAllText(rulesPath))!.AsObject();
        var rules = document["rules"]!.AsArray();
        var stagedRule = rules.OfType<JsonObject>().Single(rule => rule["packageId"]!.GetValue<string>() == "preferred.mod");
        Assert.Multiple(() =>
        {
            Assert.That(document["customProperty"]!.GetValue<string>(), Is.EqualTo("preserve this"));
            Assert.That(rules, Has.Count.EqualTo(2));
            Assert.That(stagedRule["loadTheseAfter"]!.AsArray().Select(node => node!.GetValue<string>()), Is.EqualTo(["first.mod", "second.mod"]));
        });
    }

    [Test]
    public void AddAndRemoveRule_UsesRimSortObjectRulesAndPreservesExistingRuleMetadata()
    {
        using var directory = new TemporaryDirectory();
        var rulesPath = directory.CreateFile("userRules.json", """
            {
              "timestamp": 1784748915,
              "rules": {
                "lyth.anthrosonaefelines": {
                  "loadAfter": {
                    "sk.researchicons": { "comment": "", "name": "Research Icons" }
                  }
                }
              }
            }
            """);
        var editor = new JsonRimSortUserRuleEditor();

        editor.Load(rulesPath);
        editor.AddLoadAfterRule("preferred.mod", [new RimSortLoadAfterTarget("first.mod", "First Mod")]);
        editor.Save();

        var rulesAfterAddition = JsonNode.Parse(File.ReadAllText(rulesPath))!["rules"]!.AsObject();
        Assert.Multiple(() =>
        {
            Assert.That(rulesAfterAddition["preferred.mod"]!["loadAfter"]!["first.mod"]!["comment"]!.GetValue<string>(), Is.Empty);
            Assert.That(rulesAfterAddition["preferred.mod"]!["loadAfter"]!["first.mod"]!["name"]!.GetValue<string>(), Is.EqualTo("First Mod"));
            Assert.That(rulesAfterAddition["lyth.anthrosonaefelines"]!["loadAfter"]!["sk.researchicons"]!["name"]!.GetValue<string>(), Is.EqualTo("Research Icons"));
        });

        Assert.That(editor.RemoveRule("lyth.anthrosonaefelines"), Is.True);
        editor.Save();

        var rulesAfterRemoval = JsonNode.Parse(File.ReadAllText(rulesPath))!["rules"]!.AsObject();
        Assert.That(rulesAfterRemoval.ContainsKey("lyth.anthrosonaefelines"), Is.False);
    }
}
