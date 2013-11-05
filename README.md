# BuildGen

BuildGen is a set of tools that allow users to generate building interiors procedurally, namely:
- Generator
- Viewer
- Editor

It also utilizes several interop formats built based on XML.
The project itself offers the following features:
- Allows the user to define building boundaries and entrances/floor changes.
- Allows the user to specify constraints that must be obeyed by the generator.
- Generates connections between the various entrances.
- Divides the interior area into zones.
- Creates multiple rooms based on each zone's area, obeying constraints.
- Places windows.
- Places doorways.
- Generates a 3D mesh of the final building.
- Generates bitmap images of each floorplan as the generator works (mostly for debugging purposes, but the final output can be used for maps for example).

## Usage

The project can be used entirely from within the editor, where you can create new building description files which define the boundaries and entrances. The generator can then be invoked on these files to generate a mesh and bitmaps of the previously defined floors. Lastly, the viewer can be used to verify the result.

## TODO
- Publish the generator (needs some code cleanup at the moment).
- Improve generator output.
-- Simplify the generated meshes.
-- Add the possibility of using custom meshes for windows, doorways, etc.
- Improve the interface.

## Development

BuildGen is published under the terms of the MIT License.

The initial version was developped as a final year project by Ivan Cebola under supervision of Fausto Mourato, for the [Polytechnic Institute of Setúbal][1] in 2013.

[1]:  http://www.ips.pt "Polytechnic Institute of Setúbal"