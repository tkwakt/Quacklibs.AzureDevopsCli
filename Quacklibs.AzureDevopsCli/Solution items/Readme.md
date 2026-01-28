# Naming conventions
cli's stand and fall with their ease of use.

## Commands
- A command is a single argument and is a logical grouping of a set of functionality.
- Sub commands specify an action that is done on that group. e.g. create | read | update | delete
  - Each sub command has one of the above names as their primary name and one of the following short names c | r | u | d
- Commands / subcommands are separated by a space. 

## Options
- Options are separated by an single dash '-' or double dash '--'
	- The single dish is for options that are denoted by a shortname (e.g. o), the double dash is for the full named options.
	   - Each and every options has both a shortname and a fullname 

azdevops [Command] [Subcommand] [--option | -o]