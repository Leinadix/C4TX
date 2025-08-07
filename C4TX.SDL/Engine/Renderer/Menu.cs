using C4TX.SDL.KeyHandler;
using C4TX.SDL.Models;
using Clay_cs;
using static C4TX.SDL.Engine.GameEngine;
using SDL;
using static SDL.SDL3;
using System.Numerics;

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

        static Vector2 wheeltotal = new Vector2(0, 0);

        public static void RenderMenu()
        {
            Clay.SetLayoutDimensions(new Clay_Dimensions(RenderEngine._windowWidth, RenderEngine._windowHeight));
            Clay.SetPointerState(mousePosition, mouseDown);
            Clay.UpdateScrollContainers(true, mouseScroll, (float)_deltaTime);

            wheeltotal += mouseScroll * 1000f * (float)_deltaTime;


            var _contentBackgroundColor = new Clay_Color(30, 30, 40, 255);

            using (Clay.Element(new()
            {
                id = Clay.Id("OuterContainer"),
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
            using (Clay.Element(new()
            {
                id = Clay.Id($"MapItem#{map.GetHashCode()}"),
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
                    textColor = new Clay_Color(255, 255, 255),
                });
            }
        }

        private static void NRenderSetItem(BeatmapSet set, int index)
        {
            using (Clay.Element(new()
            {
                id = Clay.Id($"SetItem#{set.GetHashCode()}"),
                backgroundColor =  index == 0 ? new Clay_Color(63, 61, 71) :  new Clay_Color(23, 21, 31),
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

                if (index != 0) return;

                int bmc = _availableBeatmapSets![_selectedSetIndex].Beatmaps.Count;

                int startIndex = 0;

                int endIndex = bmc;

                for (int i = startIndex; i < endIndex; i++)
                {
                    var map = _availableBeatmapSets[_selectedSetIndex].Beatmaps[i];
                    NRenderMapItem(map, i);
                }
            }
        }

        private static void NDrawSongSelectionPanel()
        {
            using (Clay.Element(new()
            {
                id = Clay.Id("SondSelectionPanel"),
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
                    childOffset = wheeltotal
                }
            }))
            {
                // Draw 5 top and 5 bottom of currently selected Set with wraparound

                int startIndex = _selectedSetIndex - 5;
                if (startIndex < 0)
                {
                    startIndex += _availableBeatmapSets.Count;
                }

                int endIndex = _selectedSetIndex + 5;
                if (endIndex >= _availableBeatmapSets.Count)
                {
                    endIndex -= _availableBeatmapSets.Count;
                }

                int count = _availableBeatmapSets.Count;

                for (int i = startIndex; i != endIndex; i = (i + 1) % count)
                {
                    // compute how many steps we've taken from startIndex, 0-based, across the wrap
                    int offset = (i - startIndex + count) % count;

                    // now offset goes  0,1,2,… even when i wraps from count-1 back to 0
                    int relativeIndex = offset - 5;

                    var set = _availableBeatmapSets[i];
                    if (set == null) continue;
                    NRenderSetItem(set, relativeIndex);
                }
            }
        }

        private unsafe static void NDrawSongInfoPanel()
        {
            using (Clay.Element(new()
            {
                id = Clay.Id("SongInfoPanel"),
                backgroundColor = new Clay_Color(23, 21, 31),
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    sizing = new Clay_Sizing(Clay_SizingAxis.Percent(0.5f), Clay_SizingAxis.Grow()),
                    padding = Clay_Padding.All(16),
                    childGap = 16,
                }
            }))
            {

                IntPtr backgroundTexture = IntPtr.Zero;

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
                        _currentMenuBackgroundTexture = LoadBackgroundTexture(beatmapDir, _currentBeatmap.BackgroundFilename);
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

                    backgroundTexture = LoadBackgroundTexture(bgDir, bgFilename);
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
                                backgroundTexture = LoadBackgroundTexture(_availableBeatmapSets[_selectedSetIndex].DirectoryPath, imageFile);
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
                float imgWidth = 0, imgHeight = 0;

                SDL_GetTextureSize((SDL_Texture*)backgroundTexture,  &imgWidth,&imgHeight);

                using (Clay.Element(new()
                {
                    id = Clay.Id("SongInfoPanelHeader"),
                    backgroundColor = new Clay_Color(230, 21, 31),
                    layout = new()
                    {
                        layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                        sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Percent(0.33f)),
                        padding = Clay_Padding.All(16),
                        childGap = 16,
                    },
                }))
                {
                    
                }
                Clay.OpenTextElement("werarawr", new()
                {
                    textAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER,
                });
                NDrawScoresPanel();
            }
        }

        private static void NDrawScoresPanel()
        {
            using (Clay.Element(new()
            {
                id = Clay.Id("ScorePanel"),
                backgroundColor = new Clay_Color(230, 21, 31),
                layout = new()
                {
                    layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
                    sizing = new Clay_Sizing(Clay_SizingAxis.Grow(), Clay_SizingAxis.Grow()),
                    padding = Clay_Padding.All(16),
                    childGap = 16,
                },
            }))
            {

            }
        }

        private static void NDrawInstructionPanel()
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
                Clay.OpenTextElement("↑/↓: Navigate songs", new Clay_TextElementConfig
                {
                    fontSize = 16,
                    textColor = new Clay_Color(255, 255, 255),
                });
                Clay.OpenTextElement("←/→: Select difficulty", new Clay_TextElementConfig
                {
                    fontSize = 16,
                    textColor = new Clay_Color(255, 255, 255),
                });
                Clay.OpenTextElement("Enter: Play selected song", new Clay_TextElementConfig
                {
                    fontSize = 16,
                    textColor = new Clay_Color(255, 255, 255),
                });
                Clay.OpenTextElement($"v{GameEngine.Version} | U: Auto-Update", new Clay_TextElementConfig
                {
                    fontSize = 16,
                    textColor = new Clay_Color(255, 255, 255),
                });
            }
        }

        public static void RenderMenuOld()
        {
            // Draw background
            DrawMenuBackground();
            
            // Draw song selection panel
            int songPanelWidth = _windowWidth * 3 / 4;
            int songPanelHeight = _windowHeight - 220; // Reduced to give more space for controls panel
            int songPanelX = (_windowWidth - songPanelWidth) / 2;
            int songPanelY = 130;
            
            // Draw song selection panel
            DrawPanel(songPanelX, songPanelY, songPanelWidth, songPanelHeight, Color._panelBgColor, Color._primaryColor);
            
            // Draw song selection content
            if (_availableBeatmapSets != null && _availableBeatmapSets.Count > 0)
            {
                int contentY = songPanelY + 20;
                int contentHeight = songPanelHeight - 60;
                
                // Draw song selection with new layout
                DrawSongSelectionIntern(songPanelX + PANEL_PADDING, contentY,
                    songPanelWidth - (2 * PANEL_PADDING), contentHeight);
            }
            else
            {
                // No songs found message
                RenderText("No beatmaps found", _windowWidth / 2, songPanelY + 150, Color._errorColor, false, true);
                RenderText("Place beatmaps in the Songs directory", _windowWidth / 2, songPanelY + 180, Color._mutedTextColor, false, true);
            }
            
            // Draw instruction panel at the bottom with increased height
            DrawInstructionPanel(songPanelX, songPanelY + songPanelHeight + 10, songPanelWidth, 80);

            // Draw the profile info panel in top right corner
            DrawProfilePanel();
        }

        #region old


        public static unsafe void DrawMenuBackground()
        {
            // Calculate gradient based on animation time to slowly shift colors
            double timeOffset = (_menuAnimationTime / 10000.0) % 1.0;
            byte colorPulse = (byte)(155 + Math.Sin(timeOffset * Math.PI * 2) * 30);

            // Top gradient color - dark blue
            SDL_Color topColor = new SDL_Color() { r = 15, g = 15, b = 35, a = 255 };
            // Bottom gradient color - slightly lighter with pulse
            SDL_Color bottomColor = new SDL_Color() { r = 30, g = 30, b = colorPulse, a = 255 };

            // Draw gradient by rendering a series of horizontal lines
            int steps = 20;
            int stepHeight = _windowHeight / steps;

            for (int i = 0; i < steps; i++)
            {
                double ratio = (double)i / steps;

                // Linear interpolation between colors
                byte r = (byte)(topColor.r + (bottomColor.r - topColor.r) * ratio);
                byte g = (byte)(topColor.g + (bottomColor.g - topColor.g) * ratio);
                byte b = (byte)(topColor.b + (bottomColor.b - topColor.b) * ratio);

                SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, r, g, b, 255);

                SDL_FRect rect = new SDL_FRect
                {
                    x = 0,
                    y = i * stepHeight,
                    w = _windowWidth,
                    h = stepHeight + 1 // +1 to avoid any gaps
                };

                SDL_RenderFillRect((SDL_Renderer*)_renderer, & rect);
            }
        }
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
        public static unsafe void DrawSongSelectionIntern(int x, int y, int width, int height)
        {
            if (_availableBeatmapSets == null || _availableBeatmapSets.Count == 0)
                return;

            // Split the area into left panel (songs list) and right panel (details)
            int leftPanelWidth = width / 2;
            int rightPanelWidth = width - leftPanelWidth - PANEL_PADDING;
            int rightPanelX = x + leftPanelWidth + PANEL_PADDING;

            // Draw left panel - song list with difficulties
            DrawSongListPanel(x, y, leftPanelWidth, height);

            // Draw top right panel - song details
            int detailsPanelHeight = height / 2 - PANEL_PADDING / 2;
            DrawSongDetailsPanel(rightPanelX, y, rightPanelWidth, detailsPanelHeight);

            // Draw bottom right panel - scores
            int scoresPanelY = y + detailsPanelHeight + PANEL_PADDING;
            int scoresPanelHeight = height - detailsPanelHeight - PANEL_PADDING;
            DrawScoresPanel(rightPanelX, scoresPanelY, rightPanelWidth, scoresPanelHeight);
        }
        public static unsafe void DrawSongListPanel(int x, int y, int width, int height)
        {
            // If search mode is active, draw search panel instead
            if (GameEngine._isSearching)
            {
                DrawSearchPanel(x, y, width, height);
                return;
            }
            
            // Title
            RenderText("Song Selection", x + width / 2, y, Color._primaryColor, true, true);

            // Draw panel for songs list
            DrawPanel(x, y + 20, width, height - 20, new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._panelBgColor, 0);

            if (_availableBeatmapSets == null || _availableBeatmapSets.Count == 0)
                return;

            // Constants for item heights and padding
            int itemHeight = 50; // Height for each beatmap
            int headerHeight = 40; // Height for set group headers

            // Calculate the absolute boundaries of the visible area
            int viewAreaTop = y + 25; // Top of the visible area
            int viewAreaHeight = height - 40; // Height of the visible area
            int viewAreaBottom = viewAreaTop + viewAreaHeight; // Bottom boundary

            // ---------------------------
            // PHASE 1: Measure all content and create flat list of all beatmaps with headers
            // ---------------------------

            // First, calculate total content height and positions for all items
            int totalContentHeight = 0;
            List<(int SetIndex, int DiffIndex, int StartY, int EndY, bool IsHeader)> itemPositions = new List<(int, int, int, int, bool)>();
            
            // Clear cached navigation items
            _cachedSongListItems.Clear();

            // Group the beatmaps by SetId
            Dictionary<string, List<(int SetIndex, int DiffIndex)>> groupedBeatmaps = new Dictionary<string, List<(int SetIndex, int DiffIndex)>>();
            
            // First pass: collect all beatmaps by their SetId
            for (int i = 0; i < _availableBeatmapSets.Count; i++)
            {
                var setId = _availableBeatmapSets[i].Id;
                if (!groupedBeatmaps.ContainsKey(setId))
                {
                    groupedBeatmaps[setId] = new List<(int SetIndex, int DiffIndex)>();
                }
                
                for (int j = 0; j < _availableBeatmapSets[i].Beatmaps.Count; j++)
                {
                    groupedBeatmaps[setId].Add((i, j));
                }
            }

            // Create a flat list of all beatmaps with headers
            int totalItems = 0;
            int flatCounter = 0;
            foreach (var groupEntry in groupedBeatmaps)
            {
                string setId = groupEntry.Key;
                var beatmapsInGroup = groupEntry.Value;
                
                if (beatmapsInGroup.Count == 0)
                    continue;
                
                // Get the set title for the header from the first beatmap in the group
                var firstItem = beatmapsInGroup[0];
                string setTitle = _availableBeatmapSets[firstItem.SetIndex].Title;
                string setArtist = _availableBeatmapSets[firstItem.SetIndex].Artist;
                
                // Add header
                int headerStartY = totalContentHeight;
                int headerEndY = headerStartY + headerHeight;
                itemPositions.Add((firstItem.SetIndex, -1, headerStartY, headerEndY, true)); // Use -1 for DiffIndex to indicate a header
                
                // Add header to navigation list (Type 0 = Header)
                _cachedSongListItems.Add((flatCounter, 0));
                
                totalContentHeight += headerHeight;
                totalItems++;
                flatCounter++;
                
                // Add all beatmaps in this group
                foreach (var beatmapInfo in beatmapsInGroup)
                {
                    // Calculate position for this beatmap
                    int beatmapStartY = totalContentHeight;
                    int beatmapEndY = beatmapStartY + itemHeight;
                    
                    // Add to positions list, false indicates it's not a header
                    itemPositions.Add((beatmapInfo.SetIndex, beatmapInfo.DiffIndex, beatmapStartY, beatmapEndY, false));
                    
                    // Add to navigation list (Type 1 = Selectable beatmap)
                    _cachedSongListItems.Add((flatCounter, 1));
                    
                    totalContentHeight += itemHeight;
                    totalItems++;
                    flatCounter++;
                }
            }

            // ---------------------------
            // PHASE 2: Calculate scroll position
            // ---------------------------

            // Find the currently selected beatmap
            int selectedItemY = 0;
            int selectedItemHeight = itemHeight;
            int flatSelectedIndex = -1;

            if (_selectedSetIndex >= 0 && _selectedSetIndex < _availableBeatmapSets.Count &&
                _selectedDifficultyIndex >= 0 && _selectedDifficultyIndex < _availableBeatmapSets[_selectedSetIndex].Beatmaps.Count)
            {
                // Find the item position for the selected beatmap
                for (int i = 0; i < itemPositions.Count; i++)
                {
                    var item = itemPositions[i];
                    if (!item.IsHeader && item.SetIndex == _selectedSetIndex && item.DiffIndex == _selectedDifficultyIndex)
                    {
                        flatSelectedIndex = i;
                        selectedItemY = item.StartY;
                        break;
                    }
                }
            }

            // Calculate max possible scroll
            int maxScroll = Math.Max(0, totalContentHeight - viewAreaHeight);

            // Center the selected item in the view
            int targetScrollPos = selectedItemY + (selectedItemHeight / 2) - (viewAreaHeight / 2);
            targetScrollPos = Math.Max(0, Math.Min(maxScroll, targetScrollPos));

            // Final scroll offset
            int scrollOffset = targetScrollPos;

            // ---------------------------
            // PHASE 3: Render items
            // ---------------------------

            // Draw each item (header or beatmap)
            for (int i = 0; i < itemPositions.Count; i++)
            {
                var item = itemPositions[i];
                
                // Calculate the actual screen Y position after applying scroll
                int screenY = viewAreaTop + item.StartY - scrollOffset;
                
                // Skip items completely outside the view area (with some buffer)
                if ((item.IsHeader && screenY + headerHeight < viewAreaTop - 50) || 
                    (!item.IsHeader && screenY + itemHeight < viewAreaTop - 50) || 
                    screenY > viewAreaBottom + 50)
                {
                    continue;
                }
                
                if (item.IsHeader)
                {
                    // Draw header
                    var setInfo = _availableBeatmapSets[item.SetIndex];
                    string headerText = $"{setInfo.Artist} - {setInfo.Title}";
                    
                    // Draw header background
                    SDL_Color headerBgColor = new SDL_Color { r = 40, g = 40, b = 70, a = 255 };
                    SDL_Color headerTextColor = new SDL_Color { r = 220, g = 220, b = 255, a = 255 };
                    
                    // Calculate proper panel height for better alignment
                    int actualHeaderHeight = headerHeight - 5;
                    DrawPanel(x + 5, screenY, width - 10, actualHeaderHeight, headerBgColor, headerBgColor, 0);
                    
                    // Truncate header text if too long
                    if (headerText.Length > 40) headerText = headerText.Substring(0, 38) + "...";
                    
                    // Render the header text
                    RenderText(headerText, x + 20, screenY + actualHeaderHeight / 2, headerTextColor, false, false, true);
                }
                else
                {
                    // Draw beatmap
                    // Check if this is the selected beatmap
                    bool isSelected = (item.SetIndex == _selectedSetIndex && item.DiffIndex == _selectedDifficultyIndex);
                    
                    // Get the current beatmap and its set
                    var beatmapSet = _availableBeatmapSets[item.SetIndex];
                    var beatmap = beatmapSet.Beatmaps[item.DiffIndex];
                    
                    // Draw beatmap background
                    SDL_Color bgColor = isSelected ? Color._primaryColor : Color._panelBgColor;
                    SDL_Color textColor = isSelected ? Color._textColor : Color._mutedTextColor;
                    
                    // Calculate proper panel height for better alignment
                    int actualItemHeight = itemHeight - 5;
                    DrawPanel(x + 20, screenY, width - 25, actualItemHeight, bgColor, isSelected ? Color._accentColor : Color._panelBgColor, isSelected ? 2 : 0);
                    
                    // Create display text showing just the difficulty (since we already show artist/title in header)
                    string beatmapTitle = $"{beatmap.Difficulty}";
                    if (beatmapTitle.Length > 30) beatmapTitle = beatmapTitle.Substring(0, 28) + "...";
                    
                    // Render beatmap text
                    RenderText(beatmapTitle, x + 35, screenY + actualItemHeight / 2, textColor, false, false);
                    
                    // Show star rating if available
                    if (beatmap.CachedDifficultyRating.HasValue && beatmap.CachedDifficultyRating.Value > 0)
                    {
                        string difficultyText = $"{beatmap.CachedDifficultyRating.Value:F2}★";
                        RenderText(difficultyText, x + width - 50, screenY + actualItemHeight / 2, textColor, false, true);
                    }
                }
            }
        }
        public static unsafe void DrawSongDetailsPanel(int x, int y, int width, int height)
        {
            // Draw panel
            DrawPanel(x, y, width, height, new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._primaryColor);

            // If no beatmaps or selection is invalid, display message
            if (_availableBeatmapSets == null || _availableBeatmapSets.Count == 0 || _selectedSetIndex < 0 || 
                (GameEngine._isSearching == false && _selectedSetIndex >= _availableBeatmapSets.Count))
            {
                RenderText("No beatmap selected", x + width / 2, y + height / 2, Color._mutedTextColor, false, true);
                return;
            }

            // If in search mode and we have a loaded beatmap, display its details directly
            if (GameEngine._isSearching && GameEngine._showSearchResults && GameEngine._currentBeatmap != null)
            {
                DisplayCurrentBeatmapDetails(x, y, width, height);
                return;
            }

            var selectedSet = _availableBeatmapSets[_selectedSetIndex];
            
            // Validate the difficulty index
            if (_selectedDifficultyIndex < 0 || _selectedDifficultyIndex >= selectedSet.Beatmaps.Count)
            {
                RenderText("Invalid beatmap selection", x + width / 2, y + height / 2, Color._mutedTextColor, false, true);
                return;
            }

            // Draw background image if available
            IntPtr backgroundTexture = IntPtr.Zero;
            
            // First try from loaded beatmap background if available
            if (_currentBeatmap != null && !string.IsNullOrEmpty(_currentBeatmap.BackgroundFilename))
            {
                var beatmapInfo = selectedSet.Beatmaps[_selectedDifficultyIndex];
                string beatmapDir = Path.GetDirectoryName(beatmapInfo.Path) ?? string.Empty;
                
                // If we haven't loaded this background yet, or it's a different one
                string cacheKey = $"{beatmapDir}_{_currentBeatmap.BackgroundFilename}";
                if (_lastLoadedBackgroundKey != cacheKey || _currentMenuBackgroundTexture == IntPtr.Zero)
                {
                    // Load the background image from the beatmap directory
                    _currentMenuBackgroundTexture = LoadBackgroundTexture(beatmapDir, _currentBeatmap.BackgroundFilename);
                    _lastLoadedBackgroundKey = cacheKey;
                }
                
                backgroundTexture = _currentMenuBackgroundTexture;
            }
            
            // Fallback to using set background if needed
            if (backgroundTexture == IntPtr.Zero && !string.IsNullOrEmpty(selectedSet.BackgroundPath)) 
            {
                // Try to load directly from BackgroundPath
                string bgDir = Path.GetDirectoryName(selectedSet.BackgroundPath) ?? string.Empty;
                string bgFilename = Path.GetFileName(selectedSet.BackgroundPath);
                
                backgroundTexture = LoadBackgroundTexture(bgDir, bgFilename);
            }
            
            // Additional fallback - search in the song directory
            if (backgroundTexture == IntPtr.Zero && !string.IsNullOrEmpty(selectedSet.DirectoryPath))
            {
                // Try to find any image file in the song directory
                try
                {
                    string[] imageExtensions = { "*.jpg", "*.jpeg", "*.png", "*.bmp" };
                    foreach (var ext in imageExtensions)
                    {
                        var imageFiles = Directory.GetFiles(selectedSet.DirectoryPath, ext);
                        if (imageFiles.Length > 0)
                        {
                            string imageFile = Path.GetFileName(imageFiles[0]);
                            backgroundTexture = LoadBackgroundTexture(selectedSet.DirectoryPath, imageFile);
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

            // Draw the background if it was loaded successfully
            if (backgroundTexture != IntPtr.Zero)
            {
                DrawBackgroundImage(backgroundTexture, x, y, width, height);
            }

            var currentBeatmap = selectedSet.Beatmaps[_selectedDifficultyIndex];
            
            // Use cached values from GameEngine instead of querying the database on every frame
            string creatorName = "";
            double bpmValue = 0;
            double lengthValue = 0;
            
            if (GameEngine._hasCachedDetails)
            {
                // Use the cached values
                creatorName = GameEngine._cachedCreator;
                bpmValue = GameEngine._cachedBPM;
                lengthValue = GameEngine._cachedLength;
            }
            else
            {
                // As a fallback, get values directly (should only happen once)
                var dbDetails = GameEngine._beatmapService.DatabaseService.GetBeatmapDetails(currentBeatmap.Id, selectedSet.Id);
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
                creatorName = selectedSet.Creator;
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

            // Draw beatmap title and difficulty
            int titleY = y + 30;
            string fullTitle = $"{currentBeatmap.Difficulty}";
            RenderText(fullTitle, x + width / 2, titleY, Color._highlightColor, false, true, true);

            // Draw artist - title
            int artistY = titleY + 30;
            RenderText(selectedSet.Artist + " - " + selectedSet.Title, x + width / 2, artistY, Color._textColor, false, true, true);
            
            // Draw mapper information
            int creatorY = artistY + 30;
            string creatorText = "Mapped by " + (string.IsNullOrEmpty(creatorName) ? "Unknown" : creatorName);
            RenderText(creatorText, x + width / 2, creatorY, Color._textColor, false, true, true);
            
            // Draw length with rate applied
            int lengthY = creatorY + 30;
            string lengthText = lengthValue > 0 ? MillisToTime(lengthValue / GameEngine._currentRate).ToString() : "--:--";
            RenderText(lengthText, x + width / 2, lengthY, Color._textColor, false, true, true);
            
            // Draw BPM with rate applied
            int rateY = lengthY + 30;
            string bpmText = bpmValue > 0 ? (bpmValue * GameEngine._currentRate).ToString("F2") + " BPM" : "--- BPM";
            RenderText(bpmText, x + width / 2, rateY, Color._textColor, false, true, true);
            
            // Draw difficulty rating
            int diffY = rateY + 30;
            double difficultyRating = 0;
            
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
                string diffText = $"{difficultyRating:F2}★";
                RenderText(diffText, x + width / 2, diffY, Color._textColor, false, true, true);
            }
            else
            {
                RenderText("No difficulty rating", x + width / 2, diffY, Color._mutedTextColor, false, true, true);
            }
        }
        private static unsafe void DisplayCurrentBeatmapDetails(int x, int y, int width, int height)
        {
            // Draw background image if available
            IntPtr searchBackgroundTexture = IntPtr.Zero;
            
            // Get the background from the currently loaded beatmap
            if (GameEngine._currentBeatmap != null && !string.IsNullOrEmpty(GameEngine._currentBeatmap.BackgroundFilename))
            {
                string beatmapDir = Path.GetDirectoryName(AudioEngine._currentAudioPath) ?? string.Empty;
                
                if (!string.IsNullOrEmpty(beatmapDir))
                {
                    // If we haven't loaded this background yet, or it's a different one
                    string cacheKey = $"{beatmapDir}_{GameEngine._currentBeatmap.BackgroundFilename}";
                    if (_lastLoadedBackgroundKey != cacheKey || _currentMenuBackgroundTexture == IntPtr.Zero)
                    {
                        // Load the background image from the beatmap directory
                        _currentMenuBackgroundTexture = LoadBackgroundTexture(beatmapDir, GameEngine._currentBeatmap.BackgroundFilename);
                        _lastLoadedBackgroundKey = cacheKey;
                    }
                    
                    searchBackgroundTexture = _currentMenuBackgroundTexture;
                }
            }
            
            // Draw the background if it was loaded successfully
            if (searchBackgroundTexture != IntPtr.Zero)
            {
                DrawBackgroundImage(searchBackgroundTexture, x, y, width, height);
            }
            
            // Draw beatmap title and difficulty
            int searchTitleY = y + 30;
            string searchFullTitle = $"{GameEngine._currentBeatmap.Version}";
            RenderText(searchFullTitle, x + width / 2, searchTitleY, Color._highlightColor, false, true, true);

            // Draw artist - title
            int searchArtistY = searchTitleY + 30;
            RenderText(GameEngine._currentBeatmap.Artist + " - " + GameEngine._currentBeatmap.Title, x + width / 2, searchArtistY, Color._textColor, false, true, true);
            
            // Draw mapper information
            int searchCreatorY = searchArtistY + 30;
            string searchCreatorText = "Mapped by " + (string.IsNullOrEmpty(GameEngine._currentBeatmap.Creator) ? "Unknown" : GameEngine._currentBeatmap.Creator);
            RenderText(searchCreatorText, x + width / 2, searchCreatorY, Color._textColor, false, true, true);
            
            // Draw length with rate applied
            int searchLengthY = searchCreatorY + 30;
            string searchLengthText = GameEngine._currentBeatmap.Length > 0 ? 
                MillisToTime(GameEngine._currentBeatmap.Length / GameEngine._currentRate).ToString() : "--:--";
            RenderText(searchLengthText, x + width / 2, searchLengthY, Color._textColor, false, true, true);
            
            // Draw BPM with rate applied
            int searchRateY = searchLengthY + 30;
            string searchBpmText = GameEngine._currentBeatmap.BPM > 0 ? 
                (GameEngine._currentBeatmap.BPM * GameEngine._currentRate).ToString("F2") + " BPM" : "--- BPM";
            RenderText(searchBpmText, x + width / 2, searchRateY, Color._textColor, false, true, true);
            
            // Draw difficulty information
            if (GameEngine._difficultyRatingService != null && GameEngine._currentBeatmap != null)
            {
                int searchDiffY = searchRateY + 30;
                double currentRating = GameEngine._difficultyRatingService.CalculateDifficulty(GameEngine._currentBeatmap, GameEngine._currentRate);
                string searchDiffText = $"{currentRating:F2}★ ({GameEngine._currentRate:F2}x)";
                RenderText(searchDiffText, x + width / 2, searchDiffY, Color._highlightColor, false, true);
            }
        }
        private static unsafe void DrawBackgroundImage(IntPtr backgroundTexture, int x, int y, int width, int height)
        {
            // Get texture dimensions

            float imgWidth, imgHeight;

            SDL_GetTextureSize((SDL_Texture*)backgroundTexture, &imgWidth, &imgHeight);
            
            // Calculate aspect ratio to maintain proportions
            float imgAspect = (float)imgWidth / imgHeight;
            float panelAspect = (float)width / height;
            
            SDL_FRect destRect;
            if (imgAspect > panelAspect)
            {
                // Image is wider than panel
                int scaledHeight = (int)(width / imgAspect);
                destRect = new SDL_FRect
                {
                    x = x,
                    y = y + (height - scaledHeight) / 2,
                    w = width,
                    h = scaledHeight
                };
            }
            else
            {
                // Image is taller than panel
                int scaledWidth = (int)(height * imgAspect);
                destRect = new SDL_FRect
                {
                    x = x + (width - scaledWidth) / 2,
                    y = y,
                    w = scaledWidth,
                    h = height
                };
            }
            
            // Draw the background image
            SDL_RenderTexture((SDL_Renderer*)_renderer, (SDL_Texture*)backgroundTexture, null, & destRect);
            
            // Add a semi-transparent dark overlay for better text readability
            SDL_FRect overlayRect = new SDL_FRect
            {
                x = x,
                y = y,
                w = width,
                h = height
            };
            
            SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, 0, 0, 0, 180);
            SDL_RenderFillRect((SDL_Renderer*)_renderer, & overlayRect);
        }
        public static unsafe void DrawScoresPanel(int x, int y, int width, int height)
        {
            DrawPanel(x, y, width, height, new SDL_Color { r = 25, g = 25, b = 45, a = 255 }, Color._primaryColor);

            // Title
            RenderText("Previous Scores", x + width / 2, y + PANEL_PADDING, Color._highlightColor, true, true);

            if (string.IsNullOrWhiteSpace(_username))
            {
                RenderText("Set username to view scores", x + width / 2, y + height / 2, Color._mutedTextColor, false, true);
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
                // Get the map hash for the selected beatmap
                string mapHash = string.Empty;

                if (_currentBeatmap != null && !string.IsNullOrEmpty(_currentBeatmap.MapHash))
                {
                    mapHash = _currentBeatmap.MapHash;
                }
                else
                {
                    // Calculate hash if needed
                    mapHash = _beatmapService.CalculateBeatmapHash(currentBeatmap.Path);
                }

                if (string.IsNullOrEmpty(mapHash))
                {
                    RenderText("Cannot load scores: Map hash unavailable", x + width / 2, y + height / 2, Color._mutedTextColor, false, true);
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
                    _hasLoggedCacheHit = false; // Reset for new hash
                    _hasCheckedCurrentHash = true; // Mark that we've checked this hash
                }
                else if (!_hasLoggedCacheHit)
                {
                    Console.WriteLine($"[DEBUG] Using cached scores for map hash: {mapHash} (found {_cachedScores.Count})");
                    _hasLoggedCacheHit = true; // Only log once per hash
                }

                // Get a copy of the cached scores (to sort without modifying the cache)
                var scores = _cachedScores.ToList();

                if (scores.Count == 0)
                {
                    RenderText("No previous plays", x + width / 2, y + height / 2, Color._mutedTextColor, false, true);
                    return;
                }

                // Header row
                int headerY = y + PANEL_PADDING + 30;
                int columnSpacing = (width / 5);

                RenderText("Date", x + PANEL_PADDING, headerY, Color._primaryColor, false, false);
                RenderText("Score", 50 + x + PANEL_PADDING + columnSpacing, headerY, Color._primaryColor, false, false);
                RenderText("Accuracy", x + PANEL_PADDING + columnSpacing * 2, headerY, Color._primaryColor, false, false);
                RenderText("Combo", x + PANEL_PADDING + columnSpacing * 3, headerY, Color._primaryColor, false, false);
                RenderText("Rate", x + PANEL_PADDING + columnSpacing * 4, headerY, Color._primaryColor, false, false);

                // Draw scores table
                int scoreY = headerY + 25;
                int rowHeight = 25;
                int maxScores = Math.Min(scores.Count, (height - 100) / rowHeight);

                // Draw table separator
                SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, Color._mutedTextColor.r, Color._mutedTextColor.g, Color._mutedTextColor.b, 100);
                SDL_FRect separator = new SDL_FRect { x = x + PANEL_PADDING, y = headerY + 15, w = width - PANEL_PADDING * 2, h = 1 };
                SDL_RenderFillRect((SDL_Renderer*)_renderer, & separator);

                for (int i = 0; i < maxScores; i++)
                {
                    var score = scores[i];

                    // Determine if this row is selected in the scores section
                    bool isScoreSelected = _isScoreSectionFocused && i == _selectedScoreIndex;

                    // Draw row background if selected
                    if (isScoreSelected)
                    {
                        SDL_SetRenderDrawColor((SDL_Renderer*)_renderer, Color._primaryColor.r, Color._primaryColor.g, Color._primaryColor.b, 100);
                        SDL_FRect rowBg = new SDL_FRect
                        {
                            x = x + PANEL_PADDING - 5,
                            y = scoreY - 5,
                            w = width - (PANEL_PADDING * 2) + 10,
                            h = rowHeight + 4
                        };
                        SDL_RenderFillRect((SDL_Renderer*)_renderer, & rowBg);
                    }

                    // Choose row color
                    SDL_Color rowColor;
                    if (i == 0)
                        rowColor = Color._highlightColor; // Gold for best
                    else if (i == 1)
                        rowColor = new SDL_Color() { r = 192, g = 192, b = 192, a = 255 }; // Silver for second best
                    else if (i == 2)
                        rowColor = new SDL_Color() { r = 205, g = 127, b = 50, a = 255 }; // Bronze for third
                    else
                        rowColor = Color._textColor;

                    var sr = -1; // _difficultyRatingService.CalculateDifficulty(_currentBeatmap, score.PlaybackRate);

                    // Format data
                    string date = score.DatePlayed.ToString("yyyy-MM-dd:HH:mm:ss");
                    string scoreText = (sr * score.Accuracy).ToString("F4");
                    string accuracy = score.Accuracy.ToString("P2");
                    string combo = $"{score.MaxCombo}x";

                    // Draw row
                    RenderText(date, x + PANEL_PADDING, scoreY, rowColor, false, false);
                    RenderText(scoreText, 50 + x + PANEL_PADDING + columnSpacing, scoreY, rowColor, false, false);
                    RenderText(accuracy, x + PANEL_PADDING + columnSpacing * 2, scoreY, rowColor, false, false);
                    RenderText(combo, x + PANEL_PADDING + columnSpacing * 3, scoreY, rowColor, false, false);
                    RenderText(score.PlaybackRate.ToString("F1"), x + PANEL_PADDING + columnSpacing * 4, scoreY, rowColor, false, false);
                    scoreY += rowHeight;
                }
            }
            catch (Exception ex)
            {
                RenderText($"Error: {ex.Message}", x + width / 2, y + height / 2, Color._errorColor, false, true);
            }
        }
        public static unsafe void DrawInstructionPanel(int x, int y, int width, int height)
        {
            // Draw panel
            DrawPanel(x, y, width, height, new SDL_Color { r = 25, g = 25, b = 35, a = 255 }, Color._primaryColor);
            
            // Draw instruction text in a grid
            int padding = 20;
            int columnWidth = (width - (3 * padding)) / 2;
            
            // Left column
            int leftX = x + padding;
            int rightX = x + width - padding - columnWidth;
            int firstRowY = y + 25;
            int secondRowY = y + 50;
            
            // Key instructions
            RenderText("↑/↓: Navigate songs", leftX, firstRowY, Color._mutedTextColor, false, false);
            RenderText("←/→: Select difficulty", leftX, secondRowY, Color._mutedTextColor, false, false);
            
            // More instructions on right
            RenderText("Enter: Play selected song", rightX, firstRowY, Color._mutedTextColor, false, false);
            
            // Include version and update check instruction
            string versionInfo = $"v{GameEngine.Version} | U: Auto-Update";
            RenderText(versionInfo, rightX, secondRowY, Color._mutedTextColor, false, false);
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
        public static unsafe void DrawProfilePanel()
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

        #endregion
    }
}
