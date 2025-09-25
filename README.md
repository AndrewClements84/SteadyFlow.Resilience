# SteadyFlow.Resilience

**Lightweight resilience toolkit for .NET**  
Retry policies · Rate limiting · Batch processing

[![Build Status](https://img.shields.io/github/actions/workflow/status/AndrewClements84/SteadyFlow.Resilience/dotnet.yml?branch=main)](https://github.com/AndrewClements84/SteadyFlow.Resilience/actions)
[![NuGet](https://img.shields.io/nuget/v/SteadyFlow.Resilience)](https://www.nuget.org/packages/SteadyFlow.Resilience)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SteadyFlow.Resilience)](https://www.nuget.org/packages/SteadyFlow.Resilience)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

---

## ✨ Features

- **Retry Policy** – automatic retries with exponential backoff  
- **Rate Limiting** – token bucket algorithm with async wait support  
- **Batch Processing** – collect items into timed or size-based batches  
- **Async-first** – designed for modern .NET apps  
- **Lightweight** – no external dependencies  

---

## 📦 Installation

Once published to NuGet:

```bash
dotnet add package SteadyFlow.Resilience
