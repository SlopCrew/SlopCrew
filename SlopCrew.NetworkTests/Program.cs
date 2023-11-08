using SlopCrew.Common;
using SlopCrew.Common.Network;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Common.Network.Serverbound;
using System.Collections;
using System.Numerics;

var useEqualsTypes = new List<Type> {
    typeof(string),
    typeof(Vector3),
    typeof(Quaternion),
};

var dummyTransform = new Transform {
    Position = new Vector3(1, 2, 3),
    Rotation = new Quaternion(4, 5, 6, 7),
    Velocity = new Vector3(8, 9, 10)
};

var playerOne = new Player {
    Name = "Big Slopper",
    ID = 1,

    Stage = 2,
    Character = 3,
    Outfit = 4,
    MoveStyle = 5,

    Transform = dummyTransform,

    IsDeveloper = true
};

var playerTwo = new Player {
    Name = "Small Slopper",
    ID = 5,

    Stage = 4,
    Character = 3,
    Outfit = 2,
    MoveStyle = 1,

    Transform = dummyTransform,

    IsDead = false,
    IsDeveloper = true
};

var packets = new List<NetworkPacket> {
    new ClientboundPlayerAnimation {
        Player = 1,
        Animation = 2,
        ForceOverwrite = false,
        Instant = true,
        AtTime = 5f
    },

    new ClientboundPlayerPositionUpdate {
        Positions = new Dictionary<uint, Transform> {
            {1, dummyTransform},
            {2, dummyTransform}
        }
    },

    new ClientboundPlayersUpdate {
        Players = new List<Player> {
            playerOne,
            playerTwo
        }
    },

    new ClientboundPlayerVisualUpdate {
        Player = 1,
        BoostpackEffect = 2,
        FrictionEffect = 3,
        Spraycan = true
    },

    new ClientboundSync {
        ServerTickActual = 1234
    },

    new ClientboundPlayerScoreUpdate {
        Multiplier = 3,
        Player = 42069,
        Score = 694201337
    },

    new ClientboundPong {
        ID = 42069
    },

    new ClientboundEncounterCancel {
        EncounterType = EncounterType.RaceEncounter
    },

    new ServerboundPing {
        ID = 42069
    },

    new ServerboundAnimation {
        Animation = 5,
        ForceOverwrite = true,
        Instant = false,
        AtTime = 10f
    },

    new ServerboundPlayerHello {
        Player = playerOne,
        SecretCode = "asdfasdfasdf"
    },

    new ServerboundPositionUpdate {
        Transform = dummyTransform
    },

    new ServerboundVisualUpdate {
        BoostpackEffect = 2,
        FrictionEffect = 3,
        Spraycan = false,
        Phone = true
    },

    new ServerboundScoreUpdate {
        Multiplier = 3,
        Score = 694201337
    },

    new ServerboundVersion {
        Version = 130,
        PluginVersion = "1.5.0"
    },

    new ServerboundEncounterRequest {
        PlayerID = 42069,
        EncounterType = EncounterType.ScoreEncounter
    },

    new ServerboundEncounterCancel {
        EncounterType = EncounterType.RaceEncounter
    },
};

foreach (var packet in packets) {
    TestPacket(packet);
}

Console.WriteLine("congration : )");

bool MatchesFields(object? a, object? b, int indent = 1) {
    if (a is null || b is null) {
        // If one is null, the other has to be null as well
        return a is null && b is null;
    }

    var aType = a.GetType();
    var bType = b.GetType();

    // That shouldn't be possible
    if (aType != bType) return false;

    var fields = a.GetType().GetFields();

    for (var i = 0; i < fields.Length; i++) {
        var field = fields[i];
        var lastField = i == fields.Length - 1;

        var indentStr = new string(' ', indent * 2);
        var checkStr = $"{indentStr}CHECK FIELD {field.Name}";
        var passStr = $"{indentStr}PASS FIELD {field.Name}";
        var failStr = $"{indentStr}FAIL FIELD {field.Name}";

        Console.WriteLine(checkStr);

        var aObj = field.GetValue(a);
        var bObj = field.GetValue(b);
        var aObjType = aObj?.GetType();
        var bObjType = bObj?.GetType();


        if (aObjType != bObjType) {
            Console.WriteLine($"{failStr} (type): expected {aObjType}, got {bObjType}");
            return false;
        }

        if (aObj is null || bObj is null) {
            var bothNull = aObj is null && bObj is null;
            if (!bothNull) {
                Console.WriteLine($"{failStr} (both null): expected {aObj}, got {bObj}");
                return false;
            }
        } else {
            if (aObjType?.IsPrimitive == true || useEqualsTypes.Contains(aObjType)) {
                if (aObj.Equals(bObj) != true) {
                    Console.WriteLine($"{failStr} (simple): expected {aObj}, got {bObj}");
                    return false;
                }
            } else {
                var genericType = aObjType.IsGenericType ? aObjType.GetGenericTypeDefinition() : null;
                var isDict = genericType == typeof(Dictionary<,>);
                var isList = genericType == typeof(List<>);
                var isEnum = aObjType.IsEnum;
                if (isDict) {
                    var aDict = (IDictionary) aObj;
                    var bDict = (IDictionary) bObj;

                    if (aDict.Count != bDict.Count) {
                        Console.WriteLine($"{failStr} (dict count): expected {aDict.Count}, got {bDict.Count}");
                        return false;
                    }

                    foreach (var key in aDict.Keys) {
                        Console.WriteLine($"{indentStr}  CHECK DICT {key}");
                        if (!MatchesFields(aDict[key], bDict[key], indent + 2)) {
                            Console.WriteLine($"{failStr} (dict): expected {aObj}, got {bObj}");
                            return false;
                        }
                    }
                } else if (isList) {
                    var aList = (IList) aObj;
                    var bList = (IList) bObj;

                    if (aList.Count != bList.Count) {
                        Console.WriteLine($"{failStr} (list count): expected {aList.Count}, got {bList.Count}");
                        return false;
                    }

                    for (var j = 0; j < aList.Count; j++) {
                        Console.WriteLine($"{indentStr}  CHECK LIST {j}");
                        if (!MatchesFields(aList[j], bList[j], indent + 2)) {
                            Console.WriteLine($"{failStr} (list): expected {aObj}, got {bObj}");
                            return false;
                        }
                    }
                } else if (isEnum) {
                    if (!aObj.Equals(bObj)) {
                        Console.WriteLine($"{failStr} (enum): expected {aObj}, got {bObj}");
                        return false;
                    }
                } else {
                    if (!MatchesFields(aObj, bObj, indent + 1)) {
                        Console.WriteLine($"{failStr} (recursive): expected {aObj}, got {bObj}");
                        return false;
                    }
                }
            }
        }

        Console.WriteLine(passStr);
        if (!lastField) Console.WriteLine();
    }

    return true;
}

void TestPacket(NetworkPacket packet) {
    var name = packet.GetType().Name;

    var serialized = packet.Serialize();
    var hex = BitConverter.ToString(serialized).Replace("-", "");
    Console.WriteLine($"CHECK {name}: {hex} ({serialized.Length} bytes)");

    var newPacket = NetworkPacket.Read(serialized);
    if (newPacket.GetType() != packet.GetType()) {
        Console.WriteLine(
            $"FAIL {name} (packet type): expected {packet.GetType().Name}, got {newPacket.GetType().Name}");
        Environment.Exit(1);
    }

    var newSerialized = newPacket.Serialize();
    if (!serialized.SequenceEqual(newSerialized)) {
        Console.WriteLine($"FAIL {name} (serialization match): expected {serialized}, got {newSerialized}");
        Environment.Exit(1);
    }

    if (!MatchesFields(packet, newPacket)) {
        Console.WriteLine($"FAIL {name} (field match): expected {packet}, got {newPacket}");
        Environment.Exit(1);
    }

    Console.WriteLine($"PASS {name}");
    Console.WriteLine();
}
