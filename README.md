# Procedural Environment Generator – Unity Game Project

This is an experimental Unity game project focused on building a real-time procedural environment generation system. The goal is to create a scalable and flexible framework that can generate complex spaces automatically while maintaining visual diversity and logical placement of elements.

## Key Features

- **Segment-Based World Structure**  
  The world is generated in modular segments that seamlessly connect, allowing for virtually infinite level expansion.

- **Controlled Randomness**  
  The system avoids pure randomness. Instead, it uses pre-defined layout patterns and logical rules to generate diverse yet coherent environments.

- **Hierarchical Prop Placement**  
  Larger structural props (desks, shelves, furniture) are placed first. Smaller decorative or functional items (monitors, books, plants, etc.) are added afterward based on context, creating natural-looking setups.

- **Repetition Avoidance**  
  A built-in system prevents the same environment segment from being spawned consecutively, increasing perceived variety.

## Project Status

Currently in early development. The core system focuses on runtime generation, seamless segment chaining, and dynamic prop distribution testing.


## Author

Developed as a systems design experiment with an emphasis on procedural generation, scalability, and gameplay potential.
