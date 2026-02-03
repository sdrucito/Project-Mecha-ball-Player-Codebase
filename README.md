This repository contains and extracted subset of the codebase from Project Mecha-Ball https://lienopc.itch.io/project-mecha-ball, a 5-month university group project developer for the course "Game Design" of Politecnico di Torino.
The goals of the project was to create a vertical slice of a original game concept and present it to professors acting as publishers, simulating an industry-style pitch and evaluation process.

---
The code in this repository has been extracted using "git filter-repo" from the original project, preserving commit history and authorship. Development of this subset was carried out collaboratively by me (as Sdrucito or s329518) and by my colleague (LienoPC), with his consent.

My main responsibilities focused on the Player system, in particular:
- input handling: unified input abstraction for different control schemes, and keyboard and gamepad support;
- player movement logic: dual movement modes with disting behaviors (physics-based rolling modes, like a sphere, and surface-walking mode, like a spider, allowing movement on walls and ceilings;
- camera handling: not included in this extract, as it was relatively less interesting.
The design goals was to create a player controller capable of switching between fundamentally different movement paradigms, while maintaining responsive controls and consistent player feedback.
