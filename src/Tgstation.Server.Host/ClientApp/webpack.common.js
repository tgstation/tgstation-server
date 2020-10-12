const HtmlWebpackPlugin = require("html-webpack-plugin");
const { CleanWebpackPlugin } = require("clean-webpack-plugin");
const CopyPlugin = require("copy-webpack-plugin");

module.exports = {
    context: __dirname,
    entry: {
        //babel: '@babel/polyfill',
        main: "./src/index.tsx"
    },

    optimization: {
        runtimeChunk: "single",
        splitChunks: {
            chunks: "all",
            cacheGroups: {
                /*
                packages: {
                    test: /[\\/]node_modules[\\/]/,
                    name: 'packages'
                },*/
                api: {
                    priority: 1,
                    test: /[\\/]ApiClient[\\/]/,
                    enforce: true
                }
            }
        }
    },

    resolve: {
        extensions: [".ts", ".tsx", ".js"],
        symlinks: false
    },

    module: {
        rules: [
            {
                test: /\.css$/,
                exclude: /node_modules/,
                loader: ["style-loader", "css-loader"],
                sideEffects: true
            },
            {
                test: /\.(scss)$/,
                use: [
                    {
                        loader: "style-loader"
                    },
                    {
                        loader: "css-loader"
                    },
                    {
                        loader: "postcss-loader",
                        options: {
                            plugins: function () {
                                return [require("autoprefixer")];
                            }
                        }
                    },
                    {
                        loader: "fast-sass-loader"
                    }
                ],
                sideEffects: true
            },
            {
                test: /\.svg$/,
                exclude: /node_modules/,
                loader: "svg-loader"
            },
            {
                test: /\.[jt]sx?$/,
                exclude: /node_modules/,
                use: {
                    loader: "babel-loader",
                    options: {
                        cacheDirectory: true,
                        babelrc: false,
                        presets: [
                            [
                                "@babel/preset-env",
                                {
                                    modules: false,
                                    exclude: ["transform-regenerator"]
                                }
                            ],
                            "@babel/preset-typescript",
                            "@babel/preset-react"
                        ],
                        plugins: [
                            ["@babel/plugin-proposal-class-properties", { loose: true }],
                            /*[
                                '@babel/plugin-transform-runtime',
                                {
                                    regenerator: false
                                }
                            ],*/
                            "react-hot-loader/babel"
                        ]
                    }
                }
            }
        ]
    },

    plugins: [
        new CleanWebpackPlugin(),
        new HtmlWebpackPlugin({
            title: "TG Server Control Panel v0.4.0",
            filename: "index.html",
            template: "src/index.html"
        }),
        new CopyPlugin({
            patterns: [
                {
                    from: "public",
                    toType: "dir"
                }
            ]
        })
    ],
    node: {
        fs: "empty"
    }
};
