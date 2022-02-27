# CognitoUserManager - an ASP.NET Core MVC implementation for Cognito User Flows

[![.NET](https://github.com/referbruv/CognitoUserManager/actions/workflows/dotnet.yml/badge.svg)](https://github.com/referbruv/CognitoUserManager/actions/workflows/dotnet.yml)
![GitHub stars](https://img.shields.io/github/stars/referbruv/CognitoUserManager)
[![Twitter Follow](https://img.shields.io/twitter/follow/referbruv?style=social&label=follow)](https://twitter.com/referbruv)

# What is this solution about?

Cognito User Manager is a user management solution that demonstrates building User flows in AWS Cognito using ASP.NET Core. 

# What is AWS Cognito?

AWS Cognito is a user management and identity service offered by AWS as a part of its Cloud suite. It provides a single user identity and authentication service which can also be used to access all of its resources. The Users are placed inside isolated spaces called User Pools, which any registered third-party client can access via OAuth flows.

# What does the solution offer?

The solution demonstrates the following features / flows in AWS Cognito:

- [x] New User SignUp
- [x] Existing User SignIn
- [x] Password Reset for a Signed In User
- [x] Forgot Password flow for an unsigned user
- [x] Fetch JWT Tokens (Id_Token, Access_Token, Refresh_Token) for a Signed In User

This solution can offer a solution to two problems:
1. It demonstrates building Cognito login flows using .NET SDK
2. It can work as a simple tool to create and manage users on a User Pool without having to go through the same process in AWS Console.

# What are the prerequisites?

The solution requires the following things first to run:
1. An active AWS Cognito User Pool
2. An active Client created on the Cognito Pool which the solution uses to connect to the User Pool
3. AccessKey and AccessSecretKey of a Programmatical User who has access to create Users

*You need to update these values inside `appsettings.json` respectively

On the environment side, the solution requires a .NET Core (.NET 5) installation on the machine.

# How do I run this?

The solution is built using ASP.NET Core (.NET 5) with a pipeline to upgrade to .NET 6 (soon), so for now you'd need a .NET Core (.NET 5) installed on your machine.

1. Clone the solution into your local repository
2. Open the solution in Visual Studio and set CognitoUserManger.WebApp as the startup project
3. Run the solution

or

1. Clone the solution into your local repository
2. Navigate to CognitoUserManager.WebApp directory and open a command prompt / Terminal 
3. Execute the command `dotnet run`

# I want to know more

To know more about this and to understand how this works in detail, I'd recommend you to check out the below articles where bits and pieces of this solution have been used:

* [Implementing Cognito User Login and Signup in ASP.NET Core using AWS SDK](https://referbruv.com/blog/posts/implementing-cognito-user-login-and-signup-in-aspnet-core-using-aws-sdk)
* [Implementing Cognito Forgot Password and Update Profile in ASP.NET Core using AWS SDK](https://referbruv.com/blog/posts/implementing-cognito-forgot-password-and-update-profile-in-aspnet-core-using-aws-sdk)

# Issues or Ideas?

If you face any issues or would like to drop a suggestion, ![raise an issue](https://github.com/referbruv/CognitoUserManager/issues/new/choose)

# License

The solution is completely open source and is licensed with MIT License.

# Show your Support 

I really hope this solution helps developers get started on building awesome things with ASP.NET Core (.NET 6) Web API and get into the world of containerized development real quick. 

Found this solution helpful and useful? You can do these to help this reach greater audience.

1. Leave a star on this repository :star:
2. Recommend this solution to your colleagues and dev community
3. Join my [Twitter family](https://twitter.com/referbruv). I regularly post awesome content on dev over there.
4. Join my [Facebook community](https://www.facebook.com/referbruv). I regularly post interesting content over there as well.
5. You can also buy me [a cup of great coffee :coffee:](https://www.buymeacoffee.com/referbruv)!

<a href="https://www.buymeacoffee.com/referbruv" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174"></a>

For more detailed articles and how-to guides, visit https://referbruv.com
