# Copilot Instructions for Auto Doc (Continued)

## Mod Overview and Purpose

**Mod Name:** Auto Doc (Continued)  
**Mod Description:** This mod is an update of the original Auto Doc mod by Ordo. Its primary purpose is to facilitate solo character playthroughs in RimWorld by simplifying and automating the surgery process, particularly for installing Bionics.

The Auto Doc mod allows players to queue surgery for a pawn, which can be completed automatically if the necessary components are adjacent. The mod introduces a specialized building resembling a cryopod, which is visually distinct from the vanilla version.

## Key Features and Systems

- **Automated Tending and Surgery:** Colonists inside the Auto Doc are automatically tended to, with surgery performed if required ingredients are nearby.
- **Texture Updates:** The cryopod's texture is modified for easy differentiation from the vanilla counterpart.
- **Open Access:** The Auto Doc can be accessed without the need for another pawn to open it.
- **Save-Support:** Progress inside the Auto Doc is now saved, ensuring continuity across game sessions.
- **Deferred Surgery Start:** Ingredients and surgeries are executed at the end of the countdown, not at the beginning.

## Coding Patterns and Conventions

- **Namespaces and Class Structures:** Follow the established class-hierarchy where each component of the mod has a dedicated class file, e.g., `AutoDocBuilding`, `CompAutoDoc`.
- **Methods **: Clearly define private methods for internal logic, e.g., `tendHediffs()`, `doSurgery()`, and public methods for external triggers or resets like `Reset()`.
- **Code Style**: Adhere to C# conventions such as camelCase for method names and PascalCase for class names.

## XML Integration

XML files in RimWorld are used to define items, buildings, and other game entities. When integrating new features or altering existing ones, ensure:

- **Consistency with XML Definitions:** Ensure that any new items or components are properly defined in XML to allow for seamless integration within the game world.
- **Balanced Stats and Costs:** Keep in mind game balance, ensuring new features are not overly powerful or costly.

## Harmony Patching

- Utilize Harmony for patching or extending the base game methods.
- Ensure patches are reversible and follow best practices by using postfix and prefix patches appropriately.
- Always test patches thoroughly to ensure they do not cause unintended side effects.

## Suggestions for Copilot

- **Suggest Code Completion:** Copilot can assist in auto-completing standard methods or debugging logic for methods like `tendHediffs()` or `doSurgery()`.
- **Provide Syntax Suggestions:** When writing Harmony patches or XML configurations, Copilot can provide syntax suggestions to streamline development.
- **Assist in Refactoring:** Use Copilot's understanding to refactor and improve existing code for better performance or readability.

By following these instructions, developers and contributors can effectively work on the Auto Doc (Continued) mod, ensuring it remains a valuable asset for RimWorld players seeking an enhanced gameplay experience.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.
