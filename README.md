# Koikatsu Card Organizer

This program is designed to organize image files for Koikatsu and related games into corresponding folders based on game type, character type, and character gender. It processes PNG files from a directory, extracts metadata, and moves the images into appropriate subfolders for easy categorization.

## How It Works
1. The program scans a specified directory and its subdirectories for PNG files.
2. It excludes directories that match game names defined in the `GameType` enum.
3. For each found PNG file, the program extracts metadata to determine:
    - **Game Type** (e.g., Koikatu, RoomGirl)
    - **Card Type** (e.g., Character, Coordinate, Studio)
    - **Character Gender** (for Character cards only)
4. Based on this information, the program moves the images into appropriately named folders for easy navigation.

### Folder Structure
The files are organized into the following folder structure based on their metadata:
- **[GameType]/**: Main directory for each game.
  - **Coordinate/**: Stores coordinate cards for the game.
  - **Studio/**: Stores studio cards.
  - **Male/**: Stores male character cards.
  - **Female/**: Stores female character cards.

### Example
If a file is identified as a Koikatu character card for a female character, it will be moved to:
```
Koikatu/Female/[FileName].png
```

## Usage
### Prerequisites
- .NET 8 SDK or higher
- Visual Studio 2022 (or any other compatible IDE)

### Building and Running
1. **Clone the Repository**  
2. **Build the Project**:
   Open the project in Visual Studio and build it in **Release** mode.  
3. **Run the Application**:
   - From Visual Studio, set the working directory to the location where your images are stored.
   - Run the application; it will scan the directory and organize the images based on the logic described above.

Alternatively, you can publish it as a single executable:
```
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```
This command will generate a single executable file that you can run without additional dependencies.