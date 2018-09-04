# CONTRIBUTING

## Introduction

Hello and welcome to /tg/station servers's contributing page. You are here because you are curious or interested in contributing - thank you! Everyone is free to contribute to this project as long as they follow the simple guidelines and specifications below; at /tg/station, we strive to maintain code stability and maintainability, and to do that, we need all pull requests to hold up to those specifications. It's in everyone's best interests - including yours! - if the same bug doesn't have to be fixed twice because of duplicated code.

First things first, we want to make it clear how you can contribute (if you've never contributed before), as well as the kinds of powers the team has over your additions, to avoid any unpleasant surprises if your pull request is closed for a reason you didn't foresee.

## Getting Started

/tg/station doesn't have a list of goals and features to add; we instead allow freedom for contributors to suggest and create their ideas for the server. That doesn't mean we aren't determined to squash bugs, which unfortunately pop up a lot due to the deep complexity of the game. Here are some useful starting guides, if you want to contribute or if you want to know what challenges you can tackle with zero knowledge about the game's code structure.

If you want to contribute the first thing you'll need to do is [set up Git](http://tgstation13.org/wiki/Setting_up_git) so you can download the source code.

You'll probably want to get a brief overview of the [server architecture](https://tgstation.github.io/tgstation-server/architecture.html) so as to better implement your change

There is an open list of approachable issues for [your inspiration here](https://github.com/tgstation/tgstation-server/issues?q=is%3Aopen+is%3Aissue+label%3A%22Good+First+Issue%22). You can also view the waffle board [here](https://waffle.io/tgstation/tgstation-server).

Here is a link to the code's always up-to-date documentation: https://tgstation.github.io/tgstation-server/annotated.html

You can of course, as always, ask for help at [#coderbus](irc://irc.rizon.net/coderbus) on irc.rizon.net. We're just here to have fun and help out, so please don't expect professional support.

### Development Environment

You need the Dotnet SDK to compile the main server and command line programs. In order to build the service version and control panel you also need a .NET 4.7.1 build chain

The recommended IDE is visual studio 2017 which has installation options for both of these.

In order to run the integration tests you must have the environment variables `TGS4_TEST_DATABASE_TYPE` set to `MySql` or `SqlServer` and `TGS4_TEST_CONNECTION_STRING` set appropriately

## Meet the Team

**Headcoder**

The Headcoder is responsible for controlling, adding, and removing maintainers from the project. In addition to filling the role of a normal maintainer, they have sole authority on who becomes a maintainer, as well as who remains a maintainer and who does not.

**Maintainers**

Maintainers are quality control. If a proposed pull request doesn't meet the following specifications, they can request you to change it, or simply just close the pull request. Maintainers are required to give a reason for closing the pull request.

Maintainers can revert your changes if they feel they are not worth maintaining or if they did not live up to the quality specifications.

## Specifications

As mentioned before, you are expected to follow these specifications in order to make everyone's lives easier. It'll save both your time and ours, by making sure you don't have to make any changes and we don't have to ask you to. Thank you for reading this section!

### Object Oriented Code
As C# is an object-oriented language, code must be object-oriented when possible in order to be more flexible when adding content to it. If you don't know what "object-oriented" means, we highly recommend you do some light research to grasp the basics.

### Tabs, not spaces
You must use tabs to indent your code, NOT SPACES.

(You may use spaces to align something, but you should tab to the block level first, then add the remaining spaces)

### No hacky code
Hacky code, such as adding specific checks, is highly discouraged and only allowed when there is ***no*** other option. (Protip: 'I couldn't immediately think of a proper way so thus there must be no other option' is not gonna cut it here! If you can't think of anything else, say that outright and admit that you need help with it. Maintainers exist for exactly that reason.)

You can avoid hacky code by using object-oriented methodologies, such as overriding a function (called "procs" in DM) or sectioning code into functions and then overriding them as required.

### No duplicated code
Copying code from one place to another may be suitable for small, short-time projects, but /tg/station is a long-term project and highly discourages this.

Instead you can use object orientation, or simply placing repeated code in a function, to obey this specification easily.

### No magic numbers or strings
This means stuff like having a "mode" variable for an object set to "1" or "2" with no clear indicator of what that means. Make these `const string`s with a name that more clearly states what it's for. This is clearer and enhances readability of your code! Get used to doing it!

### Class Design Guidelines

DO:

- Use the sealed keyword where possible
- Use the readonly keyword where possible
- Use 1 line bodies (void X() => Y();) where possible (Excluding constructors)
- Use the factory pattern where reasonable
- Use the const keyword where possible
- Use the var keyword where possible
- Use the static keyword on member functions where possible
- Use CancellationTokens where possible
- Throw appropriate ArgumentExceptions for public functions

DON'T:

- Use the private keyword
- Use the internal keyword
- Use the static keyword on fields where avoidable
- Use the public keyword where avoidable
- Handle Tasks in a synchronous fashion

### Versioning

The version format we use is 4.\<major\>.\<minor\>.\<patch\>. The first number never changes and TGS 1/2/3/4 are to be considered seperate products. The numbers that follow are the semver. The criteria for changing a version number is as follows

- Major: A breaking change to the DMAPI
- Minor: Additions or changes to the API or DMAPI or feature additions to the service
- Patch: Non-breaking changes internal to each of the 3 modules (Service, API, DMAPI)

### Formatting

The formatting style is the same one Visual studio uses by default. Quick formatting of code blocks can be achieved by deleting and retyping the trailing `}`

### Use early return
Do not enclose a function in an if-block when returning on a condition is more feasible
This is bad:
```C#
void Hello()
{
	if (thing1)
		if (!thing2)
			if (thing3 == 30)
				do stuff
}
```
This is good:
```C#
void Hello() 
{
	if (!thing1)
		return;
	if (thing2)
		return;
	if (thing3 != 30)
		return;
	do stuff
}
```
This prevents nesting levels from getting deeper then they need to be.

### Other Notes
* Code should be modular where possible; if you are working on a new addition, then strongly consider putting it in its own file unless it makes sense to put it with similar ones.

* You are expected to help maintain the code that you add, meaning that if there is a problem then you are likely to be approached in order to fix any issues, runtimes, or bugs.

## Pull Request Process

There is no strict process when it comes to merging pull requests. Pull requests will sometimes take a while before they are looked at by a maintainer; the bigger the change, the more time it will take before they are accepted into the code. Every team member is a volunteer who is giving up their own time to help maintain and contribute, so please be courteous and respectful. Here are some helpful ways to make it easier for you and for the maintainers when making a pull request.

* Make sure your pull request complies to the requirements outlined in [this guide](http://tgstation13.org/wiki/Getting_Your_Pull_Accepted) (with the exception of point 3)

* You are going to be expected to document all your changes in the pull request and add/update XML documentation comments for the functions and classes you modify. Failing to do so will mean delaying it as we will have to question why you made the change. On the other hand, you can speed up the process by making the pull request readable and easy to understand, with diagrams or before/after data.

* If you are proposing multiple changes, which change many different aspects of the code, you are expected to section them off into different pull requests in order to make it easier to review them and to deny/accept the changes that are deemed acceptable.

* If your pull request is accepted, the code you add no longer belongs exclusively to you but to everyone; everyone is free to work on it, but you are also free to support or object to any changes being made, which will likely hold more weight, as you're the one who added the feature. It is a shame this has to be explicitly said, but there have been cases where this would've saved some trouble.

* Please explain why you are submitting the pull request, and how you think your change will be beneficial to the server. Failure to do so will be grounds for rejecting the PR.

* Commits MUST be properly titled and commented as we only use merge commits for the pull request process

## Banned content
Do not add any of the following in a Pull Request or risk getting the PR closed:
* National Socialist Party of Germany content, National Socialist Party of Germany related content, or National Socialist Party of Germany references

Just becuase something isn't on this list doesn't mean that it's acceptable. Use common sense above all else.

## A word on Git
Yes, we know that the files have a tonne of mixed Windows and Linux line endings. Attempts to fix this have been met with less than stellar success, and as such we have decided to give up caring until there comes a time when it matters.
