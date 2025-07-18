using System;
using System.Collections.Generic;
using System.Linq;

namespace Spawn_Monsters.Monsters
{
    public class MonsterList
    {
        private Dictionary<string, string[]> monstersDictionary = new Dictionary<string, string[]>() {
                {"Slimes",
                new string[] {
                    "Green Slime",
                    "Frost Jelly",
                    "Red Sludge",
                    "Purple Sludge",
                    "Yellow Slime",
                    "Black Slime",
                    "Gray Sludge",
                    "Big Slime",
                    "Prismatic Slime",
                    // 🆕 NEW BIG SLIME VARIANTS  
                    "Big Blue Slime",
                    "Big Red Slime",
                    "Big Purple Slime",
                    // 🆕 NEW SLIME TYPES
                    "Tiger Slime"
                }},
                {"Bats",
                new string[] {
                    "Bat",
                    "Frost Bat",
                    "Lava Bat",
                    "Iridium Bat"
                }},
                {"Bugs",
                new string[] {
                    "Bug",
                    "Armored Bug"
                }},
                {"Flies",
                new string[] {
                    "Cave Fly",
                    "Grub",
                    "Mutant Fly",
                    "Mutant Grub"
                }},
                {"Ghosts",
                new string[] {
                    "Ghosts",
                    "Carbon Ghost",
                    "Putrid Ghost"
                }},
                {"Crabs",
                new string[] {
                    "Rock Crab",
                    "Lava Crab",
                    "Iridium Crab",
                    // 🆕 NEW CRAB-LIKE
                    "False Magma Cap"
                }},
                {"Golems",
                new string[] {
                    "Stone Golem",
                    "Wilderness Golem",
                    // 🆕 NEW GOLEMS
                    "Rock Golem",
                    "Iridium Golem"
                }},
                {"Serpents",
                new string[] {
                    "Royal Serpent",
                    "Serpent"
                }},
                {"Shadows",
                new string[] {
                    "Shadow Brute",
                    "Shadow Shaman",
                    // 🆕 NEW SHADOW VARIANT
                    "Shadow Sniper"
                }},
                {"Magma Sprites",
                new string[] {
                    "Magma Sprite",
                    "Magma Sparker"
                }},
                {"Squids",
                new string[] {
                    "Blue Squid",
                    "Squid Kid"
                }},
                {"Skeletons",
                new string[] {
                    "Skeleton",
                    "Skeleton Mage"
                }},
                {"Other",
                new string[] {
                    "Cursed Doll",
                    "Duggy",
                    "Dust Sprite",
                    "Dwarvish Sentry",
                    "Haunted Skull",
                    "Hot Head",
                    "Lava Lurk",
                    "Metal Head",
                    "Mummy",
                    "Pepper Rex",
                    "Shooter",
                    "Spider",
                    // 🆕 NEW SPIDER-LIKE
                    "Spiker"
                }}
            };

        public override string ToString()
        {
            return String.Join(
               "\n\n",
               monstersDictionary.Select(kvp =>
                   $"{kvp.Key}:\n\t" +
                   $"{String.Join("\n\t", kvp.Value)}"
               )
            );
        }
    }
}