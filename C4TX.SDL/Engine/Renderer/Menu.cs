using C4TX.SDL.KeyHandler;
using C4TX.SDL.Models;
using Clay_cs;
using static C4TX.SDL.Engine.GameEngine;
using SDL;
using static SDL.SDL3;
using System.Numerics;
using System.Runtime.InteropServices;
using C4TX.SDL.LUI;
using C4TX.SDL.LUI.Core;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;

namespace C4TX.SDL.Engine.Renderer
{
    public partial class RenderEngine
    {
        private static ClayStringCollection _clayString = new ClayStringCollection();
        private static Document[] _documents = [
            new Document
            {
                Title = _clayString.Get("Squirrels"),
                Contents = _clayString.Get(
                    """The Secret Life of Squirrels: Nature's Clever Acrobats\n""Squirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n""\n""Master Tree Climbers\n""At the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\n""But it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n""\n""Food Hoarders Extraordinaire\n""Squirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\n""Interestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n""\n""The Great Squirrel Debate: Urban vs. Wild\n""While squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\n""There is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n""\n""A Symbol of Resilience\n""In many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\n""In the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.The Secret Life of Squirrels: Nature's Clever Acrobats\n""Squirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n""\n""Master Tree Climbers\n""At the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\n""But it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n""\n""Food Hoarders Extraordinaire\n""Squirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\n""Interestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n""\n""The Great Squirrel Debate: Urban vs. Wild\n""While squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\n""There is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n""\n""A Symbol of Resilience\n""In many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\n""In the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.The Secret Life of Squirrels: Nature's Clever Acrobats\n""Squirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n""\n""Master Tree Climbers\n""At the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\n""But it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n""\n""Food Hoarders Extraordinaire\n""Squirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\n""Interestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n""\n""The Great Squirrel Debate: Urban vs. Wild\n""While squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\n""There is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n""\n""A Symbol of Resilience\n""In many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\n""In the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.The Secret Life of Squirrels: Nature's Clever Acrobats\n""Squirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n""\n""Master Tree Climbers\n""At the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\n""But it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n""\n""Food Hoarders Extraordinaire\n""Squirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\n""Interestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n""\n""The Great Squirrel Debate: Urban vs. Wild\n""While squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\n""There is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n""\n""A Symbol of Resilience\n""In many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\n""In the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.The Secret Life of Squirrels: Nature's Clever Acrobats\n""Squirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n""\n""Master Tree Climbers\n""At the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\n""But it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n""\n""Food Hoarders Extraordinaire\n""Squirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\n""Interestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n""\n""The Great Squirrel Debate: Urban vs. Wild\n""While squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\n""There is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n""\n""A Symbol of Resilience\n""In many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\n""In the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.The Secret Life of Squirrels: Nature's Clever Acrobats\n""Squirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n""\n""Master Tree Climbers\n""At the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\n""But it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n""\n""Food Hoarders Extraordinaire\n""Squirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\n""Interestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n""\n""The Great Squirrel Debate: Urban vs. Wild\n""While squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\n""There is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n""\n""A Symbol of Resilience\n""In many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\n""In the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.The Secret Life of Squirrels: Nature's Clever Acrobats\n""Squirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n""\n""Master Tree Climbers\n""At the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\n""But it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n""\n""Food Hoarders Extraordinaire\n""Squirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\n""Interestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n""\n""The Great Squirrel Debate: Urban vs. Wild\n""While squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\n""There is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n""\n""A Symbol of Resilience\n""In many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\n""In the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.The Secret Life of Squirrels: Nature's Clever Acrobats\n""Squirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n""\n""Master Tree Climbers\n""At the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\n""But it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n""\n""Food Hoarders Extraordinaire\n""Squirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\n""Interestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n""\n""The Great Squirrel Debate: Urban vs. Wild\n""While squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\n""There is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n""\n""A Symbol of Resilience\n""In many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\n""In the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.The Secret Life of Squirrels: Nature's Clever Acrobats\n""Squirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n""\n""Master Tree Climbers\n""At the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\n""But it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n""\n""Food Hoarders Extraordinaire\n""Squirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\n""Interestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n""\n""The Great Squirrel Debate: Urban vs. Wild\n""While squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\n""There is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n""\n""A Symbol of Resilience\n""In many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\n""In the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.The Secret Life of Squirrels: Nature's Clever Acrobats\n""Squirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n""\n""Master Tree Climbers\n""At the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\n""But it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n""\n""Food Hoarders Extraordinaire\n""Squirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\n""Interestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n""\n""The Great Squirrel Debate: Urban vs. Wild\n""While squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\n""There is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n""\n""A Symbol of Resilience\n""In many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\n""In the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.The Secret Life of Squirrels: Nature's Clever Acrobats\n""Squirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n""\n""Master Tree Climbers\n""At the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\n""But it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n""\n""Food Hoarders Extraordinaire\n""Squirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\n""Interestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n""\n""The Great Squirrel Debate: Urban vs. Wild\n""While squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\n""There is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n""\n""A Symbol of Resilience\n""In many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\n""In the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.The Secret Life of Squirrels: Nature's Clever Acrobats\n""Squirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n""\n""Master Tree Climbers\n""At the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\n""But it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n""\n""Food Hoarders Extraordinaire\n""Squirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\n""Interestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n""\n""The Great Squirrel Debate: Urban vs. Wild\n""While squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\n""There is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n""\n""A Symbol of Resilience\n""In many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\n""In the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.The Secret Life of Squirrels: Nature's Clever Acrobats\n""Squirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n""\n""Master Tree Climbers\n""At the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\n""But it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n""\n""Food Hoarders Extraordinaire\n""Squirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\n""Interestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n""\n""The Great Squirrel Debate: Urban vs. Wild\n""While squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\n""There is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n""\n""A Symbol of Resilience\n""In many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\n""In the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.The Secret Life of Squirrels: Nature's Clever Acrobats\n""Squirrels are often overlooked creatures, dismissed as mere park inhabitants or backyard nuisances. Yet, beneath their fluffy tails and twitching noses lies an intricate world of cunning, agility, and survival tactics that are nothing short of fascinating. As one of the most common mammals in North America, squirrels have adapted to a wide range of environments from bustling urban centers to tranquil forests and have developed a variety of unique behaviors that continue to intrigue scientists and nature enthusiasts alike.\n""\n""Master Tree Climbers\n""At the heart of a squirrel's skill set is its impressive ability to navigate trees with ease. Whether they're darting from branch to branch or leaping across wide gaps, squirrels possess an innate talent for acrobatics. Their powerful hind legs, which are longer than their front legs, give them remarkable jumping power. With a tail that acts as a counterbalance, squirrels can leap distances of up to ten times the length of their body, making them some of the best aerial acrobats in the animal kingdom.\n""But it's not just their agility that makes them exceptional climbers. Squirrels' sharp, curved claws allow them to grip tree bark with precision, while the soft pads on their feet provide traction on slippery surfaces. Their ability to run at high speeds and scale vertical trunks with ease is a testament to the evolutionary adaptations that have made them so successful in their arboreal habitats.\n""\n""Food Hoarders Extraordinaire\n""Squirrels are often seen frantically gathering nuts, seeds, and even fungi in preparation for winter. While this behavior may seem like instinctual hoarding, it is actually a survival strategy that has been honed over millions of years. Known as \"scatter hoarding,\" squirrels store their food in a variety of hidden locations, often burying it deep in the soil or stashing it in hollowed-out tree trunks.\n""Interestingly, squirrels have an incredible memory for the locations of their caches. Research has shown that they can remember thousands of hiding spots, often returning to them months later when food is scarce. However, they don't always recover every stash some forgotten caches eventually sprout into new trees, contributing to forest regeneration. This unintentional role as forest gardeners highlights the ecological importance of squirrels in their ecosystems.\n""\n""The Great Squirrel Debate: Urban vs. Wild\n""While squirrels are most commonly associated with rural or wooded areas, their adaptability has allowed them to thrive in urban environments as well. In cities, squirrels have become adept at finding food sources in places like parks, streets, and even garbage cans. However, their urban counterparts face unique challenges, including traffic, predators, and the lack of natural shelters. Despite these obstacles, squirrels in urban areas are often observed using human infrastructure such as buildings, bridges, and power lines as highways for their acrobatic escapades.\n""There is, however, a growing concern regarding the impact of urban life on squirrel populations. Pollution, deforestation, and the loss of natural habitats are making it more difficult for squirrels to find adequate food and shelter. As a result, conservationists are focusing on creating squirrel-friendly spaces within cities, with the goal of ensuring these resourceful creatures continue to thrive in both rural and urban landscapes.\n""\n""A Symbol of Resilience\n""In many cultures, squirrels are symbols of resourcefulness, adaptability, and preparation. Their ability to thrive in a variety of environments while navigating challenges with agility and grace serves as a reminder of the resilience inherent in nature. Whether you encounter them in a quiet forest, a city park, or your own backyard, squirrels are creatures that never fail to amaze with their endless energy and ingenuity.\n""In the end, squirrels may be small, but they are mighty in their ability to survive and thrive in a world that is constantly changing. So next time you spot one hopping across a branch or darting across your lawn, take a moment to appreciate the remarkable acrobat at work a true marvel of the natural world.\n""")
            },
        ];
        private static int _selectedDocumentIndex;

        static Vector2 deltaScroll = new Vector2(0, 0);

        public static Clay_ElementId publicId;

        public static void RenderMenu()
        {
            Clay.SetLayoutDimensions(new Clay_Dimensions(RenderEngine._windowWidth, RenderEngine._windowHeight));
            Clay.SetPointerState(mousePosition, mouseDown);
            Clay.UpdateScrollContainers(true, mouseScroll, (float)_deltaTime);

            deltaScroll = mouseScroll * 1000f * (float)_deltaTime;

            var _contentBackgroundColor = new Clay_Color(30, 30, 40, 255);

            publicId = Clay.Id("OuterContainer");

            using (Clay.Element(new()
            {
                id = publicId,
                backgroundColor = new Clay_Color(43, 41, 51),
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                    padding = Clay_Padding.All(16),
                    childGap = 16,
                },
            }))
            {
                using(Clay.Element(new()
                {
                    id = Clay.Id("LRContainer"),
                    layout = new()
                    {
                        layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                        sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                        padding = Clay_Padding.All(16),
                        childGap = 16,
                    }
                }))
                {
                    NDrawSongSelectionPanel();
                    NDrawSongInfoPanel();
                }
                NDrawInstructionPanel();
                NDrawProfilePanel();
            }


            RenderText("<insert new UI>", _windowWidth / 2, _windowHeight / 2, new SDL_Color()
            {
                r = 255,
                g = 255,
                b = 255,
                a = 255
            }, true, true);
        }


        private static void NRenderMapItem(BeatmapInfo map, int index)
        {
            var sid = Clay.Id($"MapItem#{map.GetHashCode()}");
            using (Clay.Element(new()
            {
                id = sid,
                backgroundColor = index == _selectedDifficultyIndex ? new Clay_Color(53, 51, 61) : new Clay_Color(23, 21, 31),
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow(54f, 100f)),
                    padding = Clay_Padding.All(16),
                    childGap = 0,
                }
            }))
            {

                Clay.OpenTextElement(map.Difficulty, new Clay_TextElementConfig
                {
                    fontSize = 20,
                    textColor = new Clay_Color(255, (Wrapper.IsHovered(sid, mousePosition) || _selectedDifficultyIndex == index ? 255.0f : 0.0f), 255),
                });

                if (Wrapper.IsHovered(sid, mousePosition) && mouseDown && !mouseDownLastframe)
                {
                    if (index == _selectedDifficultyIndex)
                    {
                        TriggerEnterGame();
                    } else
                    {
                        _selectedDifficultyIndex = index;
                        TriggerMapReload();
                    }
                }
            }
        }

        private static void NRenderSetItem(BeatmapSet set, int index)
        {
            var sid = Clay.Id($"SetItem#{set.GetHashCode()}");
            using (Clay.Element(new()
            {
                id = sid,
                backgroundColor =  index == _selectedSetIndex ? new Clay_Color(63, 61, 71) : 
                    Wrapper.IsHovered(sid, mousePosition) ? new Clay_Color(43, 41, 51) : new Clay_Color(23, 21, 31),
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                    padding = Clay_Padding.All(16),
                    childGap = 16,
                }
            }))
            {
                Clay.OpenTextElement(set.Title, new Clay_TextElementConfig
                {
                    fontSize = 20,
                    textColor = new Clay_Color(255, 255, 255),
                });

                if (index != _selectedSetIndex)
                {

                    if (Wrapper.IsHovered(sid, mousePosition) && mouseDown)
                    {
                        _selectedSetIndex = index;
                        _selectedDifficultyIndex = 0;
                        TriggerMapReload();
                    }

                    return;
                }

                int bmc = _availableBeatmapSets![_selectedSetIndex].Beatmaps.Count;

                int startIndex = 0;

                int endIndex = bmc;

                for (int i = startIndex; i < endIndex; i++)
                {
                    var map = _availableBeatmapSets[_selectedSetIndex].Beatmaps[i];
                    if (map == null) continue;
                    NRenderMapItem(map, i);
                }

                
            }
        }
        class ScrollState
        {
            public Vector2 scroll;
            public bool hover;
        }

        static Vector2 scrollVelocity;

        static Dictionary<Clay_ElementId, ScrollState> scrollStates = new ();
        private static unsafe void NDrawSongSelectionPanel()
        {
            Clay_ElementId sId = Clay.Id("SondSelectionPanel");
            if (!scrollStates.ContainsKey(sId))
            {
                scrollStates.Add(sId, new());
            }
            using (Clay.Element(new()
            {
                id = sId,
                backgroundColor = new Clay_Color(23, 21, 31),
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    sizing = new Clay_Sizing(Clay_SizingAxis.Percent(0.5f), Clay_SizingAxis.Grow()),
                    padding = Clay_Padding.All(16),
                    childGap = 1,
                },
                clip = new()
                {
                    vertical = true,
                    horizontal = false,
                    childOffset = scrollStates[sId].scroll
                }
            }))
            {
                if(Wrapper.IsHovered(sId, mousePosition))
                {
                    scrollVelocity += deltaScroll;
                    scrollStates[sId].scroll += scrollVelocity;
                    scrollStates[sId].hover = true;
                }

                if (scrollVelocity.LengthSquared() > 1)
                {
                    scrollVelocity *= (float)_deltaTime * 10f;
                } else
                {
                    scrollVelocity = new Vector2(0, 0);
                }

                // Draw all.

                int startIndex = 0;

                int count = _availableBeatmapSets.Count;

                for (int i = startIndex; i < count; i++)
                {
                    var set = _availableBeatmapSets[i];
                    if (set == null) continue;
                    NRenderSetItem(set, i);
                }
            }
        }

        private unsafe static void NDrawSongInfoPanel()
        {
            var sid = Clay.Id("SongInfoPanel");
            var width = 0.0f;
            var height = 0.0f;
            using (Clay.Element(new()
            {
                id = sid,
                backgroundColor = new Clay_Color(23, 21, 31),
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    sizing = new Clay_Sizing(Clay_SizingAxis.Percent(0.5f), Clay_SizingAxis.Grow()),
                    padding = new Clay_Padding
                    {
                        left = 16,
                        right = 16,
                        top = 16,
                        bottom = 16
                    },
                    childGap = 16,
                }
            }))
            {
                width = Clay.GetElementData(sid).boundingBox.width;
                height = Clay.GetElementData(sid).boundingBox.height * 0.5f;

                IntPtr backgroundTexture = IntPtr.Zero;

                #region loadingShenanigans

                // First try from loaded beatmap background if available
                if (_currentBeatmap != null && !string.IsNullOrEmpty(_currentBeatmap.BackgroundFilename))
                {
                    var beatmapInfo = _availableBeatmapSets[_selectedSetIndex].Beatmaps[_selectedDifficultyIndex];
                    string beatmapDir = Path.GetDirectoryName(beatmapInfo.Path) ?? string.Empty;

                    // If we haven't loaded this background yet, or it's a different one
                    string cacheKey = $"{beatmapDir}_{_currentBeatmap.BackgroundFilename}";
                    if (_lastLoadedBackgroundKey != cacheKey || _currentMenuBackgroundTexture == IntPtr.Zero)
                    {
                        // Load the background image from the beatmap directory
                        _currentMenuBackgroundTexture = LoadBackgroundTexture(beatmapDir, _currentBeatmap.BackgroundFilename, width, height);
                        _lastLoadedBackgroundKey = cacheKey;
                    }

                    backgroundTexture = _currentMenuBackgroundTexture;
                }

                // Fallback to using set background if needed
                if (backgroundTexture == IntPtr.Zero && !string.IsNullOrEmpty(_availableBeatmapSets[_selectedSetIndex].BackgroundPath))
                {
                    // Try to load directly from BackgroundPath
                    string bgDir = Path.GetDirectoryName(_availableBeatmapSets[_selectedSetIndex].BackgroundPath) ?? string.Empty;
                    string bgFilename = Path.GetFileName(_availableBeatmapSets[_selectedSetIndex].BackgroundPath);

                    backgroundTexture = LoadBackgroundTexture(bgDir, bgFilename, width, height);
                }

                // Additional fallback - search in the song directory
                if (backgroundTexture == IntPtr.Zero && !string.IsNullOrEmpty(_availableBeatmapSets[_selectedSetIndex].DirectoryPath))
                {
                    // Try to find any image file in the song directory
                    try
                    {
                        string[] imageExtensions = { "*.jpg", "*.jpeg", "*.png", "*.bmp" };
                        foreach (var ext in imageExtensions)
                        {
                            var imageFiles = Directory.GetFiles(_availableBeatmapSets[_selectedSetIndex].DirectoryPath, ext);
                            if (imageFiles.Length > 0)
                            {
                                string imageFile = Path.GetFileName(imageFiles[0]);

                                backgroundTexture = LoadBackgroundTexture(_availableBeatmapSets[_selectedSetIndex].DirectoryPath, imageFile, width, height);
                                if (backgroundTexture != IntPtr.Zero)
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error searching for image files: {ex.Message}");
                    }
                }
                #endregion

                Wrapper.UserData data = new() { w = (int)width - 32, h = (int)height -32 };
                IntPtr dataPtr = Marshal.AllocHGlobal(Marshal.SizeOf<LUI.Wrapper.UserData>());
                Marshal.StructureToPtr(data, dataPtr, false);


                using (Clay.Element(new()
                {
                    id = Clay.Id("SongInfoPanelHeader"),
                    backgroundColor = new Clay_Color(0, 0, 0),
                    layout = new()
                    {
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                        sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Percent(0.5f)),
                        padding = Clay_Padding.All(16),
                        childGap = 16,
                    },
                    image = new Clay_ImageElementConfig()
                    {
                        imageData = (void*)backgroundTexture
                    },
                    userData = (void*)dataPtr,
                    
                }))
                { }

                using (Clay.Element(new()
                {
                    id = Clay.Id("Organizer"),
                    layout = new()
                    {
                        layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                        sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                        childGap = 16
                    }
                }))
                {
                    var sicId = Clay.Id("SongInfoContent");
                    using (Clay.Element(new()
                    {
                        id = sicId,
                        backgroundColor = new Clay_Color(18, 16, 26),
                        layout = new()
                        {
                            layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                            sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            padding = Clay_Padding.All(16),
                            childGap = 16,
                        }
                    }))
                    {

                        Wrapper.DrawClayText("Song info", 40, System.Drawing.Color.Yellow, 0, 0, 10, Clay_TextAlignment.CLAY_TEXT_ALIGN_RIGHT, Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE, sicId);

                        var currentBeatmap = _availableBeatmapSets?[_selectedSetIndex].Beatmaps[_selectedDifficultyIndex] ?? null;


                        string creatorName = "";
                        double bpmValue = 0;
                        double lengthValue = 0;

                        if (GameEngine._hasCachedDetails)
                        {
                            creatorName = GameEngine._cachedCreator;
                            bpmValue = GameEngine._cachedBPM;
                            lengthValue = GameEngine._cachedLength;
                        }
                        else
                        {
                            var dbDetails = GameEngine._beatmapService.DatabaseService.GetBeatmapDetails(currentBeatmap.Id, _availableBeatmapSets?[_selectedSetIndex].Id);
                            creatorName = dbDetails.Creator;
                            bpmValue = dbDetails.BPM;
                            lengthValue = dbDetails.Length;

                            // Cache these values for future renders
                            GameEngine._cachedCreator = creatorName;
                            GameEngine._cachedBPM = bpmValue;
                            GameEngine._cachedLength = lengthValue;
                            GameEngine._hasCachedDetails = true;
                        }

                        // Fall back to in-memory values if we couldn't get data from the database
                        if (string.IsNullOrEmpty(creatorName))
                            creatorName = _availableBeatmapSets?[_selectedSetIndex].Creator;
                        if (bpmValue <= 0)
                            bpmValue = currentBeatmap.BPM;
                        if (lengthValue <= 0)
                            lengthValue = currentBeatmap.Length;

                        // Fall back to placeholders if all values are empty
                        if (string.IsNullOrEmpty(creatorName))
                            creatorName = "Unknown";
                        if (bpmValue <= 0 && _currentBeatmap != null && _currentBeatmap.BPM > 0)
                            bpmValue = _currentBeatmap.BPM;
                        if (lengthValue <= 0 && _currentBeatmap != null && _currentBeatmap.Length > 0)
                            lengthValue = _currentBeatmap.Length;


                        string fullTitle = $"{currentBeatmap.Difficulty}";
                        Wrapper.DrawClayText(
                            fullTitle,
                            10,
                            System.Drawing.Color.White,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NEWLINES,
                            sicId);

                        Wrapper.DrawClayText(
                            _availableBeatmapSets?[_selectedSetIndex].Artist + " - " + _availableBeatmapSets?[_selectedSetIndex].Title,
                            10,
                            System.Drawing.Color.White,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NEWLINES,
                            sicId);

                        var creatorText = "Mapped by " + (string.IsNullOrEmpty(creatorName) ? "Unknown" : creatorName);
                        Wrapper.DrawClayText(
                            creatorText,
                            10,
                            System.Drawing.Color.White,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NEWLINES,
                            sicId);

                        string lengthText = lengthValue > 0 ? MillisToTime(lengthValue / GameEngine._currentRate).ToString() : "--:--";
                        Wrapper.DrawClayText(
                            lengthText,
                            10,
                            System.Drawing.Color.White,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NEWLINES,
                            sicId);

                        string bpmText = bpmValue > 0 ? (bpmValue * GameEngine._currentRate).ToString("F2") + " BPM" : "--- BPM";
                        Wrapper.DrawClayText(
                            bpmText,
                            10,
                            System.Drawing.Color.White,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NEWLINES,
                            sicId); 

                        double difficultyRating = 0;
                        string diffText = "No difficulty rating";
                        if (currentBeatmap.CachedDifficultyRating.HasValue)
                        {
                            // Check if we need to calculate with current rate
                            if (Math.Abs(currentBeatmap.LastCachedRate - GameEngine._currentRate) > 0.01) // Small threshold for float comparison
                            {
                                // Recalculate for current rate if not already done
                                if (_currentBeatmap != null)
                                {
                                    difficultyRating = GameEngine._difficultyRatingService.CalculateDifficulty(_currentBeatmap, GameEngine._currentRate);
                                }
                                else
                                {
                                    difficultyRating = currentBeatmap.CachedDifficultyRating.Value;
                                }
                            }
                            else
                            {
                                // Use existing cached value
                                difficultyRating = currentBeatmap.CachedDifficultyRating.Value;
                            }

                            // Display the difficulty rating with rate applied
                            diffText = $"{difficultyRating:F2} *";
                        }

                        var ratingColor = GetRatingColor(difficultyRating);

                        Wrapper.DrawClayText(
                            diffText,
                            10,
                            ratingColor,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE,
                            sicId);

                    }

                    NDrawScoresPanel();
                }
            }
        }

        public static System.Drawing.Color Lerp(System.Drawing.Color a, System.Drawing.Color b, double t)
        {
            t = Math.Clamp(t, 0, 1);
            int A = (int)Math.Round(a.A + (b.A - a.A) * t);
            int R = (int)Math.Round(a.R + (b.R - a.R) * t);
            int G = (int)Math.Round(a.G + (b.G - a.G) * t);
            int B = (int)Math.Round(a.B + (b.B - a.B) * t);
            return System.Drawing.Color.FromArgb(A, R, G, B);
        }

        public static System.Drawing.Color GetRatingColor(double rating)
        {
            rating = Math.Clamp(rating, 0, 10);

            var stops = new[]
            {
            System.Drawing.Color.White,
            System.Drawing.Color.FromArgb(255,  0,255,255),
            System.Drawing.Color.FromArgb(255,  0,  0,255),
            System.Drawing.Color.FromArgb(255,  0,255,  0),
            System.Drawing.Color.FromArgb(255,255,255,  0),
            System.Drawing.Color.FromArgb(255,255,127,  0),
            System.Drawing.Color.FromArgb(255,255,  0,  0),
            System.Drawing.Color.FromArgb(255, 75,  0,130),
            System.Drawing.Color.FromArgb(255,148,  0,211),
            System.Drawing.Color.FromArgb(255,255,  0,255),
            System.Drawing.Color.Black,
        };

            int lo = (int)Math.Floor(rating);
            if (lo >= 10) return stops[10];
            double t = rating - lo;
            return Lerp(stops[lo], stops[lo + 1], t);
        }

        private static void NDrawScoreSelectionItem(ScoreData score, bool isSelected, int index)
        {
            var sicId = Clay.Id($"ScoreItem#{index}");
            using (Clay.Element(new()
            {
                id = sicId,
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                    sizing = new()
                    {
                        width = Clay_SizingAxis.Grow(),
                        height = Clay_SizingAxis.Fixed(64)
                    }
                },
                backgroundColor = new()
                {
                    a = 255,
                    r = 0,
                    g = 50,
                    b = 50,
                }
            }))
            {
                var bgC = new Clay_Color()
                {
                    a = 50,
                    r = 255,
                    g = 255,
                    b = 255,
                };
                if (isSelected)
                {
                    bgC.a = 255;
                }

                // Choose row color
                System.Drawing.Color rowColor;
                if (index == 0)
                    rowColor = System.Drawing.Color.Gold;
                else if (index == 1)
                    rowColor = System.Drawing.Color.Silver;
                else if (index == 2)
                    rowColor = System.Drawing.Color.Brown;
                else
                    rowColor = System.Drawing.Color.White;

                var sr = score.starRating;

                // Format data
                string date = score.DatePlayed.ToString("yyyy-MM-dd:HH:mm:ss");
                string scoreText = (100 * sr * 4 * Math.Max(0, score.Accuracy - 0.8)).ToString("F4");
                string accuracy = score.Accuracy.ToString("P2");
                string combo = $"{score.MaxCombo}x";
                string rate = $"{score.PlaybackRate}x";

                // Draw row
                using (Clay.Element(new()
                {
                    layout = new()
                    {
                        sizing = new()
                        {
                            width = Clay_SizingAxis.Grow(),
                            height = Clay_SizingAxis.Grow()
                        }
                    },
                    backgroundColor = bgC
                }))
                {
                    //  Username                |   100.00%
                    //                          |
                    //                          |    combo
                    //  1234-12-12-12-12-12     |   12345pp
                    //

                    using (Clay.Element(new()
                    {
                        layout = new()
                        {
                            layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                            sizing = new()
                            {
                                width = Clay_SizingAxis.Grow(),
                                height = Clay_SizingAxis.Grow()
                            }
                        }
                    }))
                    {
                        using (Clay.Element(new()
                        {
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                                sizing = new()
                                {
                                    width = Clay_SizingAxis.Percent(0.666666f),
                                    height = Clay_SizingAxis.Grow()
                                }
                            },
                        }))
                        {
                            using (Clay.Element(new()
                            {
                                layout = new()
                                {
                                    layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                    sizing = new()
                                    {
                                        width = Clay_SizingAxis.Grow(),
                                        height = Clay_SizingAxis.Grow()
                                    }
                                }
                            }))
                            {
                                Clay.OpenTextElement(score.Username, new()
                                {
                                    fontId = 0,
                                    fontSize = 10,
                                });
                            }

                            using (Clay.Element(new()
                            {
                                layout = new()
                                {
                                    layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                    sizing = new()
                                    {
                                        width = Clay_SizingAxis.Grow(),
                                        height = Clay_SizingAxis.Grow()
                                    }
                                }
                            }))
                            {
                                Clay.OpenTextElement(scoreText, new()
                                {
                                    fontId = 0,
                                    fontSize = 10,
                                });
                            }
                            using (Clay.Element(new()
                            {
                                layout = new()
                                {
                                    layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                    sizing = new()
                                    {
                                        width = Clay_SizingAxis.Grow(),
                                        height = Clay_SizingAxis.Grow(16)
                                    }
                                }
                            }))
                            {
                                Clay.OpenTextElement(date, new()
                                {
                                    fontId = 0,
                                    fontSize = 10,
                                });
                            }
                        }
                        using (Clay.Element(new()
                        {
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                                sizing = new()
                                {
                                    width = Clay_SizingAxis.Grow(),
                                    height = Clay_SizingAxis.Grow()
                                }
                            }
                        }))
                        {
                            using (Clay.Element(new()
                            {
                                layout = new()
                                {
                                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                                    sizing = new()
                                    {
                                        width = Clay_SizingAxis.Grow(),
                                        height = Clay_SizingAxis.Grow()
                                    }
                                }
                            }))
                            {
                                using (Clay.Element(new()
                                {
                                    layout = new()
                                    {
                                        sizing = new()
                                        {
                                            width = Clay_SizingAxis.Grow(),
                                            height = Clay_SizingAxis.Grow()
                                        }
                                    }
                                }))
                                {
                                    Clay.OpenTextElement(accuracy, new()
                                    {
                                        fontId = 0,
                                        fontSize = 10,
                                    });
                                }
                                    
                                using (Clay.Element(new()
                                {
                                    layout = new()
                                    {
                                        sizing = new()
                                        {
                                            width = Clay_SizingAxis.Grow(),
                                            height = Clay_SizingAxis.Grow()
                                        }
                                    }
                                }))
                                {
                                    Clay.OpenTextElement(rate, new()
                                    {
                                        fontId = 0,
                                        fontSize = 10,
                                    });
                                }
                                    
                            }
                            using (Clay.Element(new()
                            {
                                layout = new()
                                {
                                    layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                    sizing = new()
                                    {
                                        width = Clay_SizingAxis.Grow(),
                                        height = Clay_SizingAxis.Grow(16)
                                    }
                                }
                            }))
                            {
                                Clay.OpenTextElement(combo, new()
                                {
                                    fontId = 0,
                                    fontSize = 10,
                                });
                            }
                        }
                    }

                }
            }
        }

        private static void NDrawScoresPanel()
        {
            // TODO: Scrolling
            var sicId = Clay.Id("ScoresPanel");
            using (Clay.Element(new()
            {
                id = sicId,
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                    padding = Clay_Padding.All(16),
                    childGap = 16,
                },
            }))
            {
                Wrapper.DrawClayText(
                    "Previous Scores",
                    40,
                    System.Drawing.Color.White,
                    0,
                    0,
                    10,
                    Clay_TextAlignment.CLAY_TEXT_ALIGN_LEFT,
                    Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE,
                    sicId);

                if (string.IsNullOrWhiteSpace(_username))
                {
                    Wrapper.DrawClayText(
                    "Set username to view scores",
                    40,
                    System.Drawing.Color.Red,
                    0,
                    0,
                    10,
                    Clay_TextAlignment.CLAY_TEXT_ALIGN_LEFT,
                    Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE,
                    sicId);

                    return;
                }

                if (_availableBeatmapSets == null || _selectedSetIndex >= _availableBeatmapSets.Count)
                    return;

                var currentMapset = _availableBeatmapSets[_selectedSetIndex];

                if (_selectedDifficultyIndex >= currentMapset.Beatmaps.Count)
                    return;

                var currentBeatmap = currentMapset.Beatmaps[_selectedDifficultyIndex];

                try
                {
                    string mapHash = string.Empty;

                    if (_currentBeatmap != null && !string.IsNullOrEmpty(_currentBeatmap.MapHash))
                    {
                        mapHash = _currentBeatmap.MapHash;
                    }
                    else
                    {
                        mapHash = _beatmapService.CalculateBeatmapHash(currentBeatmap.Path);
                    }

                    if (string.IsNullOrEmpty(mapHash))
                    {
                        Wrapper.DrawClayText(
                            "Cannot load scores: Map hash unavailable",
                            40,
                            System.Drawing.Color.Red,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_LEFT,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE,
                            sicId);
                        return;
                    }

                    // Get scores for this beatmap using the hash
                    if (mapHash != _cachedScoreMapHash || !_hasCheckedCurrentHash)
                    {
                        // Cache miss - fetch scores from service
                        Console.WriteLine($"[DEBUG] Cache miss - fetching scores for map hash: {mapHash}");
                        _cachedScores = _scoreService.GetBeatmapScoresByHash(_username, mapHash);
                        _cachedScores = _cachedScores.OrderByDescending(s => _difficultyRatingService.CalculateDifficulty(GameEngine._currentBeatmap, s.PlaybackRate) * s.Accuracy).ToList();
                        _cachedScoreMapHash = mapHash;
                        _hasLoggedCacheHit = false;
                        _hasCheckedCurrentHash = true;
                    }
                    else if (!_hasLoggedCacheHit)
                    {
                        Console.WriteLine($"[DEBUG] Using cached scores for map hash: {mapHash} (found {_cachedScores.Count})");
                        _hasLoggedCacheHit = true;
                    }

                    var scores = _cachedScores.OrderByDescending(obj => obj.starRating * 4 * Math.Max(0, obj.Accuracy - 0.8)).ToList();


                    if (scores.Count == 0)
                    {
                        Wrapper.DrawClayText(
                            "No replays found!",
                            40,
                            System.Drawing.Color.Yellow,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_LEFT,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE,
                            sicId);
                        return;
                    }

                    for (int i = 0; i < _cachedScores.Count; i++)
                    {
                        var score = scores[i];
                        // Determine if this row is selected in the scores section
                        bool isScoreSelected = _isScoreSectionFocused && i == _selectedScoreIndex;

                        NDrawScoreSelectionItem(score, isScoreSelected, i);
                    }
                }
                catch (Exception ex)
                {
                    Wrapper.DrawClayText(
                            $"Error: {ex.Message}",
                            40,
                            System.Drawing.Color.Yellow,
                            0,
                            0,
                            10,
                            Clay_TextAlignment.CLAY_TEXT_ALIGN_LEFT,
                            Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE,
                            sicId);
                }
            }
        }

        private static unsafe void NDrawInstructionPanel()
        {
            using (Clay.Element(new()
            {
                id = Clay.Id("InstructionSeperator"),
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                }
            }))
            {
                using (Clay.Element(new()
                {
                    id = Clay.Id("InstructionSeperatorTop"),
                    layout = new()
                    {
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                        sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                    }
                }))
                {
                    using (Clay.Element(new()
                    {
                        id = Clay.Id("InstructionFooter"),
                        backgroundColor = new Clay_Color(23, 21, 31),
                        layout = new()
                        {
                            layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                            sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Fixed(100)),
                            padding = Clay_Padding.All(16),
                            childGap = 16,
                        }
                    }))
                    {
                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("↑/↓: Change Set", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                                textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
                            });
                        }
                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("←/→: Change difficulty", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                            });
                        }

                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("Enter: Play", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                            });
                        }
                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("Tab: Switch Menu", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                            });
                        }

                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("P: Switch Profile", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                                textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
                            });
                        }
                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("1/2: Change Rate", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                                textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
                            });
                        }
                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("S: Settings", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                                textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
                            });
                        }
                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement("F5: Reload Maps", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                                textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
                            });
                        }
                        using (Clay.Element(new()
                        {
                            backgroundColor = new Clay_Color(23, 21, 31),
                            layout = new()
                            {
                                layoutDirection = Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
                                sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            }
                        }))
                        {
                            Clay.OpenTextElement($"U: Check for updates", new Clay_TextElementConfig
                            {
                                fontSize = 16,
                                textColor = new Clay_Color(255, 255, 255),
                            });
                        }

                    }
                }
                using (Clay.Element(new()
                {
                    id = Clay.Id("InstructionSeperatorBot"),
                    layout = new()
                    {
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                        sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                    }
                }))
                {

                    string text = $"v{GameEngine.Version}";
                    int size, width, height;
                    var bytes = StringToUtf8(text, out size);

                    SDL3_ttf.TTF_GetStringSize((TTF_Font*)_font, (byte*)&bytes, (nuint)size, &width, &height);

                    using (Clay.Element(new()
                    {
                        layout = new()
                        {
                            layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                            sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                            padding = Clay_Padding.Hor((ushort)((ushort)(RenderEngine._windowWidth / 2) - width)),
                        }
                    }))
                    {
                        Clay.OpenTextElement(text, new()
                        {
                            fontId = 0,
                            fontSize = 32
                        });
                    }
                        
                }

            }

            
        }

        private static void NDrawProfilePanel()
        {
            const int panelWidth = 300;
            const int panelHeight = 300;
            int panelX = _windowWidth - panelWidth - PANEL_PADDING;
            int panelY = PANEL_PADDING;

            DrawPanel(panelX, panelY, panelWidth, panelHeight, Color._panelBgColor, Color._accentColor);

            // Draw header
            SDL_Color titleColor = new SDL_Color() { r = 255, g = 255, b = 255, a = 255 };
            SDL_Color subtitleColor = new SDL_Color() { r = 200, g = 200, b = 255, a = 255 };
            RenderText("C4TX", panelX + panelWidth / 2, panelY + 50, titleColor, true, true);
            RenderText("A 4k Rhythm Game", panelX + panelWidth / 2, panelY + 80, subtitleColor, false, true);

            // Draw current profile
            if (!string.IsNullOrWhiteSpace(_username))
            {
                // Show current profile
                SDL_Color profileColor = new SDL_Color() { r = 150, g = 200, b = 255, a = 255 };
                RenderText("Current Profile:", panelX + panelWidth / 2, panelY + 130, Color._textColor, false, true);
                RenderText(_username, panelX + panelWidth / 2, panelY + 155, profileColor, false, true);
                RenderText("Press P to switch profile", panelX + panelWidth / 2, panelY + 180, Color._mutedTextColor, false, true);
            }
            else
            {
                // Prompt to select a profile
                SDL_Color warningColor = new SDL_Color() { r = 255, g = 150, b = 150, a = 255 };
                RenderText("No profile selected", panelX + panelWidth / 2, panelY + 130, warningColor, false, true);
                RenderText("Press P to select a profile", panelX + panelWidth / 2, panelY + 155, Color._textColor, false, true);
            }

            // Draw menu instructions
            RenderText("Press S for Settings", panelX + panelWidth / 2, panelY + 210, Color._mutedTextColor, false, true);
            RenderText("Press F11 for Fullscreen", panelX + panelWidth / 2, panelY + 235, Color._mutedTextColor, false, true);
        }

        #region old

        public static unsafe void DrawHeader(string title, string subtitle)
        {
            // Draw game logo/title
            RenderText(title, _windowWidth / 2, 50, Color._accentColor, true, true);

            // Draw subtitle
            RenderText(subtitle, _windowWidth / 2, 90, Color._mutedTextColor, false, true);

            // Draw a horizontal separator line
            SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, Color._primaryColor.r, Color._primaryColor.g, Color._primaryColor.b, 150);
            SDL_FRect separatorLine = new SDL_FRect
            {
                x = _windowWidth / 4,
                y = 110,
                w = _windowWidth / 2,
                h = 2
            };
            SDL_RenderFillRect((SDL_Renderer*)_renderer, & separatorLine);
        }
        public static unsafe void DrawSearchPanel(int x, int y, int width, int height)
        {
            // Title
            RenderText("Song Search", x + width / 2, y, Color._primaryColor, true, true);
            
            // Draw panel for search and results
            DrawPanel(x, y + 20, width, height - 20, new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._panelBgColor, 0);
            
            // Draw search input field
            int inputFieldY = y + 40;
            SDL_Color inputBgColor = new SDL_Color { r = 20, g = 20, b = 40, a = 255 };
            SDL_Color inputBorderColor = GameEngine._isSearchInputFocused
                ? new SDL_Color { r = 100, g = 200, b = 255, a = 255 }
                : new SDL_Color { r = 100, g = 100, b = 255, a = 255 };
                
            DrawPanel(x + 20, inputFieldY, width - 40, 40, inputBgColor, inputBorderColor);
            
            // Draw search query with cursor if focused
            string displayQuery = GameEngine._isSearchInputFocused ? GameEngine._searchQuery + "_" : GameEngine._searchQuery;
            if (string.IsNullOrEmpty(displayQuery))
            {
                displayQuery = GameEngine._isSearchInputFocused ? "_" : "Search...";
            }
            
            RenderText(displayQuery, x + 40, inputFieldY + 20, Color._textColor, false, false);
            
            // Draw help text
            SDL_Color helpColor = new SDL_Color { r = 180, g = 180, b = 180, a = 255 };
            RenderText("Press Enter to search, Escape to exit", x + width / 2, inputFieldY + 50, helpColor, false, true);
            
            // Draw results if search has been performed
            if (GameEngine._showSearchResults && GameEngine._searchResults != null)
            {
                // Draw results header
                int resultsY = inputFieldY + 70;
                int resultsCount = 0;
                
                // Count total beatmaps in results
                foreach (var set in GameEngine._searchResults)
                {
                    if (set.Beatmaps != null)
                    {
                        resultsCount += set.Beatmaps.Count;
                    }
                }
                
                if (resultsCount > 0)
                {
                    RenderText($"Found {resultsCount} beatmaps", x + width / 2, resultsY, Color._primaryColor, false, true);
                    
                    // Constants for item heights and padding
                    int itemHeight = 50; // Height for each beatmap
                    int headerHeight = 40; // Height for mapset headers
                    
                    // Calculate the absolute boundaries of the visible area
                    int viewAreaTop = resultsY + 50; 
                    int viewAreaHeight = height - (viewAreaTop - y) - 10; // Height of the visible area
                    int viewAreaBottom = viewAreaTop + viewAreaHeight; // Bottom boundary
                    
                    // Calculate the flat index positions with headers
                    List<(int SetIndex, int DiffIndex, int StartY, int Height, bool IsHeader)> itemPositions = new List<(int, int, int, int, bool)>();
                    int totalContentHeight = 0;
                    
                    // Create a flat representation with headers for each set
                    for (int setIndex = 0; setIndex < GameEngine._searchResults.Count; setIndex++)
                    {
                        var set = GameEngine._searchResults[setIndex];
                        
                        if (set.Beatmaps == null || set.Beatmaps.Count == 0)
                            continue;
                            
                        // Add a header for this set
                        itemPositions.Add((setIndex, -1, totalContentHeight, headerHeight, true));
                        totalContentHeight += headerHeight;
                        
                        // Add all beatmaps in this set
                        for (int diffIndex = 0; diffIndex < set.Beatmaps.Count; diffIndex++)
                        {
                            itemPositions.Add((setIndex, diffIndex, totalContentHeight, itemHeight, false));
                            totalContentHeight += itemHeight;
                        }
                    }
                    
                    // Calculate max possible scroll
                    int maxScroll = Math.Max(0, totalContentHeight - viewAreaHeight);
                    
                    // Find the currently selected beatmap in flat representation
                    int selectedItemY = 0;
                    
                    // Get the set and diff index from flat index
                    var setDiffPosition = SearchKeyhandler.GetSetAndDiffFromFlatIndex(GameEngine._selectedSetIndex);
                    
                    if (setDiffPosition.SetIndex >= 0 && setDiffPosition.DiffIndex >= 0)
                    {
                        // Find the corresponding position in our itemPositions list
                        for (int i = 0; i < itemPositions.Count; i++)
                        {
                            var item = itemPositions[i];
                            if (!item.IsHeader && item.SetIndex == setDiffPosition.SetIndex && item.DiffIndex == setDiffPosition.DiffIndex)
                            {
                                selectedItemY = item.StartY;
                                break;
                            }
                        }
                    }
                    
                    // Center the selected item in the view
                    int targetScrollPos = selectedItemY + (itemHeight / 2) - (viewAreaHeight / 2);
                    targetScrollPos = Math.Max(0, Math.Min(maxScroll, targetScrollPos));
                    
                    // Final scroll offset
                    int scrollOffset = targetScrollPos;
                    
                    // Draw each item (header or beatmap)
                    for (int i = 0; i < itemPositions.Count; i++)
                    {
                        var item = itemPositions[i];
                        
                        // Calculate the actual screen Y position after applying scroll
                        int screenY = viewAreaTop + item.StartY - scrollOffset;
                        
                        // Skip items completely outside the view area
                        if (screenY + item.Height < viewAreaTop - 50 || screenY > viewAreaBottom + 50)
                        {
                            continue;
                        }
                        
                        if (item.IsHeader)
                        {
                            // Draw header
                            var setInfo = GameEngine._searchResults[item.SetIndex];
                            string headerText = $"{setInfo.Artist} - {setInfo.Title}";
                            
                            // Draw header background
                            SDL_Color headerBgColor = new SDL_Color { r = 40, g = 40, b = 70, a = 255 };
                            SDL_Color headerTextColor = new SDL_Color { r = 220, g = 220, b = 255, a = 255 };
                            
                            // Calculate proper panel height for better alignment
                            int actualHeaderHeight = headerHeight - 5;
                            DrawPanel(x + 5, screenY, width - 10, actualHeaderHeight, headerBgColor, headerBgColor, 0);
                            
                            // Truncate header text if too long
                            if (headerText.Length > 40) headerText = headerText.Substring(0, 38) + "...";
                            
                            // Draw header text
                            RenderText(headerText, x + 20, screenY + actualHeaderHeight / 2, headerTextColor, false, false);
                        }
                        else
                        {
                            // Draw beatmap item
                            var set = GameEngine._searchResults[item.SetIndex];
                            var beatmap = set.Beatmaps[item.DiffIndex];
                            
                            // Check if this is the currently selected beatmap
                            bool isSelected = (setDiffPosition.SetIndex == item.SetIndex && setDiffPosition.DiffIndex == item.DiffIndex);
                            
                            // Draw beatmap background
                            SDL_Color bgColor = isSelected ? Color._primaryColor : Color._panelBgColor;
                            SDL_Color textColor = isSelected ? Color._textColor : Color._mutedTextColor;
                            
                            // Calculate proper panel height for better alignment
                            int actualItemHeight = itemHeight - 5;
                            DrawPanel(x + 20, screenY, width - 25, actualItemHeight, bgColor, isSelected ? Color._accentColor : Color._panelBgColor, isSelected ? 2 : 0);
                            
                            // Create display text for difficulty
                            string difficultyText = $"[{beatmap.Difficulty}]";
                            if (difficultyText.Length > 15) difficultyText = difficultyText.Substring(0, 13) + "...]";
                            
                            // Render difficulty name
                            RenderText(difficultyText, x + 35, screenY + actualItemHeight / 2, textColor, false, false);
                            
                            // Show star rating if available
                            if (beatmap.CachedDifficultyRating.HasValue && beatmap.CachedDifficultyRating.Value > 0)
                            {
                                string starRatingText = $"{beatmap.CachedDifficultyRating.Value:F2}★";
                                RenderText(starRatingText, x + width - 50, screenY + actualItemHeight / 2, textColor, false, true);
                            }
                        }
                    }
                }
                else
                {
                    // No results found
                    RenderText("No matching beatmaps found", x + width / 2, resultsY + 40, Color._errorColor, false, true);
                }
            }
        }

        #endregion
    }
}
