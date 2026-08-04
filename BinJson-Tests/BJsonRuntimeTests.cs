#nullable enable

using System;
using System.IO;
using Krampus.BinJson;
using Krampus.BinJson.Serialization;
using Krampus.BinJson.Serialization.References;
using Xunit;

namespace Krampus.BinJson.Tests
{
    public class BJsonRuntimeTests
    {
        [Fact]
        public void Runtime_ReusesConfiguration_AcrossOperations()
        {
            var options = new BJsonSerializerOptions
            {
                NamingPolicy = NamingPolicy.SnakeCase
            };
            var runtime = new BJsonRuntime(options);

            var first = runtime.Serialize(new RuntimeModel { PlayerName = "A", Level = 1 });
            var second = runtime.Serialize(new RuntimeModel { PlayerName = "B", Level = 2 });

            Assert.True(first.ObjectValue.ContainsKey("player_name"));
            Assert.True(second.ObjectValue.ContainsKey("player_name"));
            Assert.Equal(1, first.ObjectValue["level"].IntValue);
            Assert.Equal(2, second.ObjectValue["level"].IntValue);
        }

        [Fact]
        public void Runtime_PreserveReferences_DoesNotLeakAcrossCalls()
        {
            var options = new BJsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve
            };
            var runtime = new BJsonRuntime(options);

            var node = new RuntimeNode();
            node.Next = node;

            var first = runtime.Serialize(node);
            var second = runtime.Serialize(node);

            Assert.True(first.ObjectValue.ContainsKey("$id"));
            Assert.True(second.ObjectValue.ContainsKey("$id"));

            Assert.True(first.ObjectValue.ContainsKey("Next"));
            Assert.True(second.ObjectValue.ContainsKey("Next"));

            var firstNext = first.ObjectValue["Next"].ObjectValue;
            var secondNext = second.ObjectValue["Next"].ObjectValue;

            Assert.True(firstNext.ContainsKey("$ref"));
            Assert.True(secondNext.ContainsKey("$ref"));
            Assert.Equal("1", first.ObjectValue["$id"].StringValue);
            Assert.Equal("1", second.ObjectValue["$id"].StringValue);
        }

        [Fact]
        public void Runtime_Preprocessor_ResolvesConditionalBlocks_And_Variables()
        {
            var options = new BJsonSerializerOptions
            {
                PreprocessorContext = new BJsonPreprocessorContext()
            };
            options.PreprocessorContext!.SetVariable("Platform", "PC");

            var document = BJsonValue.Create(new BJsonObject
            {
                ["$branches"] = BJsonValue.Create(new BJsonArray
                {
                    BJsonValue.Create(new BJsonObject
                    {
                        ["$if"] = BJsonValue.Create(new BJsonObject
                        {
                            ["$var"] = BJsonValue.Create("Platform"),
                            ["$eq"] = BJsonValue.Create("PC")
                        }),
                        ["$then"] = BJsonValue.Create(new BJsonObject
                        {
                            ["Mode"] = BJsonValue.Create("Ultra")
                        })
                    }),
                    BJsonValue.Create(new BJsonObject
                    {
                        ["$if"] = BJsonValue.Create(new BJsonObject
                        {
                            ["$var"] = BJsonValue.Create("Platform"),
                            ["$eq"] = BJsonValue.Create("Mobile")
                        }),
                        ["$then"] = BJsonValue.Create(new BJsonObject
                        {
                            ["Mode"] = BJsonValue.Create("Low")
                        })
                    }),
                    BJsonValue.Create(new BJsonObject
                    {
                        ["$else"] = BJsonValue.Create(new BJsonObject
                        {
                            ["Mode"] = BJsonValue.Create("Medium")
                        })
                    })
                })
            });

            var result = BJson.Deserialize<ConditionalConfig>(document, options);

            Assert.NotNull(result);
            Assert.Equal("Ultra", result!.Mode);
        }

        [Fact]
        public void Runtime_Preprocessor_ResolvesAnchors_And_ExternalReferences()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "binjson-preprocessor-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                var externalPath = Path.Combine(tempDirectory, "inventory.bjson");
                var externalDocument = new BJsonObject
                {
                    ["Name"] = BJsonValue.Create("Sword")
                };
                File.WriteAllBytes(externalPath, BJson.SerializeToBytes(BJsonValue.Create(externalDocument)));

                var document = BJsonValue.Create(new BJsonObject
                {
                    ["PrimaryColor"] = BJsonValue.Create("#FF00FF"),
                    ["Display"] = BJsonValue.Create(new BJsonObject
                    {
                        ["$ref"] = BJsonValue.Create("primaryColor")
                    }),
                    ["Inventory"] = BJsonValue.Create(externalPath)
                });

                var result = BJson.Deserialize<AnchoredAndExternalConfig>(document, new BJsonSerializerOptions());

                Assert.NotNull(result);
                Assert.Equal("#FF00FF", result!.PrimaryColor);
                Assert.Equal("#FF00FF", result.Display);
                Assert.NotNull(result.Inventory);
                Assert.Equal("Sword", result.Inventory!.Name);
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, recursive: true);
            }
        }

        [Fact]
        public void Runtime_Preprocessor_ExternalReferenceOptional_MissingFile_YieldsNull()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "binjson-preprocessor-optional-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                var options = new BJsonSerializerOptions
                {
                    PreprocessorContext = new BJsonPreprocessorContext
                    {
                        BasePath = tempDirectory
                    }
                };

                var document = BJsonValue.Create(new BJsonObject
                {
                    ["Settings"] = BJsonValue.Create("missing-settings.bjson")
                });

                var result = BJson.Deserialize<OptionalExternalConfig>(document, options);
                Assert.NotNull(result);
                Assert.Null(result!.Settings);
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, recursive: true);
            }
        }

        [Fact]
        public void Runtime_Preprocessor_ExternalReferencePathOutsideBase_Throws()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "binjson-preprocessor-policy-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                var options = new BJsonSerializerOptions
                {
                    PreprocessorContext = new BJsonPreprocessorContext
                    {
                        BasePath = tempDirectory
                    }
                };

                var document = BJsonValue.Create(new BJsonObject
                {
                    ["Inventory"] = BJsonValue.Create("..\\outside.bjson")
                });

                var exception = Assert.Throws<Krampus.BinJson.Error.BJsonDeserializationException>(() =>
                    BJson.Deserialize<AnchoredAndExternalConfig>(document, options));

                Assert.Equal(Krampus.BinJson.Error.BJsonErrorCode.ExternalReferenceSecurityViolation, exception.ErrorCodeValue);
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, recursive: true);
            }
        }

        [Fact]
        public void Runtime_ExternalReferenceSerialization_WritesFile_And_StoresPathToken()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "binjson-preprocessor-write-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                var options = new BJsonSerializerOptions
                {
                    PreprocessorContext = new BJsonPreprocessorContext
                    {
                        BasePath = tempDirectory
                    }
                };

                var model = new WriteExternalConfig
                {
                    Inventory = new ExternalInventory
                    {
                        Name = "Sword"
                    }
                };

                var serialized = BJson.Serialize(model, options);
                Assert.True(serialized.TryGetObject(out var obj));
                Assert.True(obj["Inventory"].TryGetString(out var storedPath));
                Assert.False(string.IsNullOrWhiteSpace(storedPath));
                Assert.True(File.Exists(storedPath));

                var externalValue = BJson.DeserializeFromFile(storedPath);
                var externalObject = Assert.IsType<BJsonObject>(externalValue.ObjectValue);
                Assert.Equal("Sword", externalObject["Name"].StringValue);
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                    Directory.Delete(tempDirectory, recursive: true);
            }
        }

        private sealed class RuntimeModel
        {
            public string PlayerName { get; set; } = string.Empty;

            public int Level { get; set; }
        }

        [BJsonSerializable]
        [BJsonPreprocessor]
        private sealed class ConditionalConfig
        {
            public string Mode { get; set; } = string.Empty;
        }

        [BJsonSerializable]
        [BJsonPreprocessor]
        private sealed class AnchoredAndExternalConfig
        {
            [BJsonAnchor("primaryColor")]
            public string PrimaryColor { get; set; } = string.Empty;

            public string Display { get; set; } = string.Empty;

            [BJsonExternalRef]
            public ExternalInventory? Inventory { get; set; }
        }

        private sealed class ExternalInventory
        {
            public string Name { get; set; } = string.Empty;
        }

        [BJsonSerializable]
        [BJsonPreprocessor]
        private sealed class OptionalExternalConfig
        {
            [BJsonExternalRef(Optional = true)]
            public ExternalInventory? Settings { get; set; }
        }

        [BJsonSerializable]
        private sealed class WriteExternalConfig
        {
            [BJsonExternalRef(FixedPath = "inventory-output.bjson")]
            public ExternalInventory? Inventory { get; set; }
        }

        private sealed class RuntimeNode
        {
            public RuntimeNode? Next { get; set; }
        }
    }
}
