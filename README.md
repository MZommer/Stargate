# Stargate
Stargate is a command-line tool that helps manage map bundles (Unity AssetBundles) for JDNext.

Usage
To use Stargate, open a terminal or command prompt and run the following command:
```bash
Stargate <Base Bundle> <Map folder> <Output folder>
```
Replace the following arguments:

- `<Base Bundle>`
: The path to the base bundle file to use as a starting point.

- `<Map folder>`
: The path to the folder containing the map files to include in the new bundle. This folder should contain:

    - A Pictos
    folder with the pictogram image files.

    - A MoveSpace
    folder with the classifier files.

    - `songdata.json` and `musictrack.json`
    files (which can be generated using the provided Python app).

`<Output folder>`
: The path to the folder where the new bundle file should be created.

Example
```bash
Stargate BaseBundle.bundle .\Maps\Intoxicated .\Output
```
This will create a new bundle file in the 
.\Output
 folder, using 
BaseBundle.bundle
 as the base bundle to replace with
.\Maps\Intoxicated
 folder.

## Dependencies
[AssetsTools](https://github.com/Perfare/AssetStudio) by [Prefare](https://github.com/Perfare)
You need to have the binaries in the extern folder.

SixLabors ImageSharp. You can install it using NuGet.

## TODO
- Add AssetPackage support

- Fix the cleaning functions

- Make UI

## Contributing
Contributions are welcome! Please open an issue or submit a pull request on the GitHub repository.

## License
This project is licensed under the GNU License.

Thanks to yunyl for some help.
