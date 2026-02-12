# Unity Motion Logging Simulator – Engineering Thesis

Scripts-only showcase of an engineering thesis project focused on motion analysis and logging in a Unity-based simulator environment.

The goal of the project was to design and implement a simulator capable of:
- registering user movement on a predefined track,
- logging motion parameters over time,
- preparing data for further analysis of user behavior and movement patterns.

The simulator was developed as a research tool supporting experiments related to motion tracking and prediction.

---

## Project Scope
The implementation includes:
- player movement logic and camera control,
- track segmentation and traversal detection,
- motion logging (position, velocity, orientation over time),
- utilities for describing and managing track segments,
- editor tools supporting development and testing.

The collected data was used as a basis for later analytical processing and evaluation, as described in the thesis.

---

## Architecture Overview
The project is structured around:
- a modular movement system,
- a segment-based track model,
- a logging layer responsible for exporting motion data,
- separation between simulation logic and hardware-specific input sources.

This design allows the simulation logic to remain independent from the underlying input device.

---

## Note on Hardware Integration
The original thesis project included integration with a specialized motion controller (VirtuSphere).

To avoid sharing proprietary or device-specific implementation details, the hardware integration layer (controller and GUI handlers) is intentionally excluded from this public repository.

This repository presents the author’s own implementation of simulation logic, data logging, and system architecture.

---

## Repository Contents
- `scripts/` – core simulation, movement, logging and track management scripts
- `editor/` – custom Unity editor utilities used during development
- `media/` – screenshots presenting the simulator in action

This repository is intended as a **portfolio showcase**, not a fully runnable Unity project.

---

## Technologies
- Unity (C#)
- Object-Oriented Programming
- Data logging & preprocessing
- Simulation systems
